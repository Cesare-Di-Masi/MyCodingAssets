import yaml
import gym
import resource
import random
import time
import os
import sys
import torch
import logging

from env_wrappers.resource_limiter import ResourceLimiter
from agents.cipher_rl import create_genetic_defender
from monitor.monitor import Monitor
from evolution.mutation import mutate_population
from evolution.crossover import crossover_population
from evolution.genome import Genome

# Setup logging configurazione globale, file + console
logging.basicConfig(
    level=logging.DEBUG,
    format='[%(asctime)s] %(levelname)s: %(message)s',
    handlers=[
        logging.StreamHandler(sys.stdout),
        logging.FileHandler("trainer.log", mode='w')
    ]
)
logger = logging.getLogger(__name__)

def load_config(path: str) -> dict:
    """
    Carica e valida la configurazione YAML.
    Solleva FileNotFoundError o yaml.YAMLError su errore.
    """
    if not os.path.isfile(path):
        logger.error(f"File di configurazione non trovato: {path}")
        raise FileNotFoundError(f"Config file '{path}' missing.")

    try:
        with open(path, "r") as f:
            cfg = yaml.safe_load(f)
    except yaml.YAMLError as e:
        logger.error(f"Errore parsing YAML: {e}")
        raise

    # Validazioni base
    required_keys = ["env_name", "max_cpu", "max_ram_mb", "log_dir", "checkpoint_dir"]
    for key in required_keys:
        if key not in cfg:
            logger.error(f"Configurazione mancante chiave obbligatoria: {key}")
            raise KeyError(f"Missing config key: {key}")

    return cfg

def limit_resources(max_cpu: float, max_ram_mb: int):
    """
    Imposta limiti soft a CPU (in frazione) e RAM (MB).
    Logga errori ma non interrompe se fallisce (compatibilità SO).
    """
    try:
        max_cpu_seconds = int(max_cpu * 60)
        resource.setrlimit(resource.RLIMIT_CPU, (max_cpu_seconds, max_cpu_seconds))
        max_ram_bytes = max_ram_mb * 1024 * 1024
        resource.setrlimit(resource.RLIMIT_AS, (max_ram_bytes, max_ram_bytes))
        logger.info(f"Limiti risorse applicati: CPU={max_cpu}s, RAM={max_ram_mb}MB")
    except Exception as e:
        logger.warning(f"Imposizione limiti risorse fallita: {e}")

def setup_environment(cfg: dict) -> gym.Env:
    """
    Crea ambiente e applica wrapper.
    Gestisce errori di gym.make.
    """
    try:
        env = gym.make(cfg["env_name"])
    except Exception as e:
        logger.error(f"Errore creazione ambiente Gym '{cfg['env_name']}': {e}")
        raise

    # Applica wrappers
    try:
        env = ResourceLimiter(env, cfg["max_cpu"], cfg["max_ram_mb"])
    except Exception as e:
        logger.error(f"Errore applicazione ResourceLimiter: {e}")
        raise

    logger.info(f"Ambiente '{cfg['env_name']}' creato e wrapper applicato.")
    return env

def evaluate_agent(agent, env, episodes=5) -> float:
    """
    Valuta un agente in media su più episodi.
    Gestisce eccezioni per garantire stabilità.
    """
    total_reward = 0.0
    try:
        for ep in range(episodes):
            obs = env.reset()
            done = False
            while not done:
                obs, reward, done, _ = agent.step()
                if reward is None:
                    reward = 0
                total_reward += reward
    except Exception as e:
        logger.error(f"Errore durante valutazione agente: {e}")
        # Se fallisce la valutazione, assegna punteggio minimo
        return float('-inf')
    return total_reward / episodes

def select_top_agents(population, scores, top_k):
    """
    Seleziona i migliori top_k agenti basandosi su score.
    Controlla coerenza liste.
    """
    if len(population) != len(scores):
        logger.error("Mismatch lunghezza population e scores in select_top_agents")
        raise ValueError("Population and scores length mismatch")

    scored = list(zip(population, scores))
    scored.sort(key=lambda x: x[1], reverse=True)
    selected = [agent for agent, score in scored[:top_k]]

    if not selected:
        logger.warning("Nessun agente selezionato nel top_k, verifica la selezione")

    return selected

def train_loop(cfg, monitor):
    """
    Ciclo evolutivo con logging, validazioni e gestione errori.
    """
    env = setup_environment(cfg)
    population_size = cfg.get("population_size", 10)
    generations = cfg.get("generations", 20)
    mutation_rate = cfg.get("genome", {}).get("mutation_rate", 0.2)
    top_k = max(2, population_size // 5)

    logger.info(f"Inizializzazione popolazione ({population_size} agenti)...")
    population = []
    for idx in range(population_size):
        genome_cfg = cfg.get("genome", {"key_rotation_rate": 0.5})
        genome = Genome(genome_cfg)
        agent = create_genetic_defender(env, genome.genes)
        population.append(agent)

    for gen in range(generations):
        logger.info(f"Generazione {gen + 1}/{generations}")

        # Valutazione popolazione
        scores = []
        for i, agent in enumerate(population):
            score = evaluate_agent(agent, env)
            scores.append(score)
            monitor.log(gen * population_size + i, score, cost=0)
            logger.debug(f"Agente {i} punteggio: {score}")

        # Selezione top agenti
        try:
            top_agents = select_top_agents(population, scores, top_k)
        except Exception as e:
            logger.error(f"Errore selezione top agenti: {e}")
            break

        # Generazione nuova popolazione tramite crossover e mutazione
        new_population = []
        while len(new_population) < population_size:
            parent1 = random.choice(top_agents)
            parent2 = random.choice(top_agents)

            try:
                child_genome = parent1.genome.crossover(parent2.genome)
                child_genome.mutate(mutation_rate)
                child_agent = create_genetic_defender(env, child_genome.genes)
                new_population.append(child_agent)
            except Exception as e:
                logger.error(f"Errore creazione figlio genetico: {e}")

        population = new_population

        # Salvataggio checkpoint con controllo IO
        save_dir = cfg.get("checkpoint_dir", "checkpoints")
        if not os.path.isdir(save_dir):
            try:
                os.makedirs(save_dir)
            except Exception as e:
                logger.error(f"Impossibile creare cartella checkpoint: {e}")

        save_path = os.path.join(save_dir, f"gen_{gen+1}.pth")
        try:
            save_population(population, save_path)
            logger.info(f"Checkpoint salvato: {save_path}")
        except Exception as e:
            logger.error(f"Errore salvataggio checkpoint: {e}")

def save_population(population, path):
    """
    Salva genomi in file torch.
    Assicura che gli agenti abbiano attributo genome.genes.
    """
    genomes = []
    for agent in population:
        try:
            genomes.append(agent.genome.genes)
        except AttributeError as e:
            logger.error(f"Agente senza genome.genes: {e}")
            genomes.append({})  # Placeholder vuoto

    torch.save(genomes, path)

def main():
    try:
        cfg = load_config("configs/safe_rl_config.yaml")
    except Exception as e:
        logger.critical(f"Impossibile caricare configurazione: {e}")
        sys.exit(1)

    try:
        limit_resources(cfg["max_cpu"], cfg["max_ram_mb"])
    except Exception:
        # Già gestito dentro limit_resources, continua comunque
        pass

    try:
        os.makedirs(cfg.get("checkpoint_dir", "checkpoints"), exist_ok=True)
    except Exception as e:
        logger.error(f"Errore creazione directory checkpoint: {e}")

    monitor = Monitor(cfg["log_dir"])

    try:
        train_loop(cfg, monitor)
    except Exception as e:
        logger.critical(f"Errore nel ciclo di training: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()
