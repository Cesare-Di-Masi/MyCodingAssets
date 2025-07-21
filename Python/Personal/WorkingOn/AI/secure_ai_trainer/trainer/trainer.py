import yaml
import gym
import resource
import random
import time
import os
import torch

from env_wrappers.resource_limiter import ResourceLimiter
from agents.cipher_rl import create_genetic_defender
from monitor.monitor import Monitor
from evolution.mutation import mutate_population
from evolution.crossover import crossover_population
from evolution.genome import Genome

def load_config(path: str) -> dict:
    """
    Carica la configurazione YAML da file.
    - path: percorso al file YAML.
    Ritorna: dizionario configurazione.
    """
    with open(path, "r") as f:
        return yaml.safe_load(f)

def limit_resources(max_cpu: float, max_ram_mb: int):
    """
    Imposta limiti soft di utilizzo CPU e RAM per il processo.
    - max_cpu: frazione CPU utilizzabile (es. 0.8)
    - max_ram_mb: limite RAM in megabyte
    
    Limite CPU è impostato in secondi su RLIMIT_CPU (soft/hard uguale).
    Limite RAM è impostato su RLIMIT_AS (address space).
    
    ATTENZIONE: RLIMIT_AS può non funzionare correttamente su tutti i sistemi
    e può bloccare processi in modi non immediatamente evidenti.
    """
    max_cpu_seconds = int(max_cpu * 60)  # Limite CPU in secondi totali (es. 48 sec per 0.8)
    resource.setrlimit(resource.RLIMIT_CPU, (max_cpu_seconds, max_cpu_seconds))
    max_ram_bytes = max_ram_mb * 1024 * 1024
    resource.setrlimit(resource.RLIMIT_AS, (max_ram_bytes, max_ram_bytes))

def setup_environment(cfg: dict) -> gym.Env:
    """
    Crea ambiente Gym e applica i wrapper base.
    Per ora applica solo ResourceLimiter.
    TODO: estendere con wrapper cifratura e malware.
    
    Ritorna ambiente wrapperato.
    """
    env = gym.make(cfg["env_name"])
    env = ResourceLimiter(env, cfg["max_cpu"], cfg["max_ram_mb"])
    return env

def evaluate_agent(agent, env, episodes=5) -> float:
    """
    Valuta un agente eseguendo più episodi.
    - agent: oggetto agente con metodo .step() (deve simulare passo)
    - env: ambiente Gym (utilizzato per reset)
    - episodes: numero di episodi per media
    
    Ritorna: reward medio per episodio.
    
    NOTE:
    - L'agente deve implementare `.step()` che simula un passo.
    - Qui non si passa action vera, ma l'architettura deve essere adattata.
    - Reward None o mancante è considerato zero.
    """
    total_reward = 0
    for _ in range(episodes):
        obs = env.reset()
        done = False
        while not done:
            obs, reward, done, _ = agent.step()
            total_reward += reward if reward is not None else 0
    return total_reward / episodes

def select_top_agents(population, scores, top_k):
    """
    Seleziona i top-k agenti in base ai punteggi.
    Ordina decrescente e ritorna i migliori.
    """
    scored = list(zip(population, scores))
    scored.sort(key=lambda x: x[1], reverse=True)
    selected = [agent for agent, score in scored[:top_k]]
    return selected

def train_loop(cfg, monitor):
    """
    Ciclo principale di addestramento ed evoluzione.
    - cfg: configurazione caricata
    - monitor: oggetto Monitor per logging
    
    - Crea ambiente
    - Inizializza popolazione agenti genetici
    - Per ogni generazione:
        - Valuta tutti gli agenti
        - Registra metriche su monitor
        - Seleziona top agenti
        - Riproduce nuova popolazione tramite crossover e mutazione
        - Salva checkpoint popolazione
    """
    env = setup_environment(cfg)
    population_size = cfg.get("population_size", 10)
    generations = cfg.get("generations", 20)
    mutation_rate = cfg.get("genome", {}).get("mutation_rate", 0.2)
    top_k = max(2, population_size // 5)  # almeno 2 top agenti

    # Inizializza popolazione agenti genetici con genomi dalla configurazione
    population = []
    for _ in range(population_size):
        genome = Genome(cfg.get("genome", {"key_rotation_rate": 0.5}))
        agent = create_genetic_defender(env, genome.genes)
        population.append(agent)

    for gen in range(generations):
        print(f"Generation {gen + 1}/{generations}")

        # Valutazione
        scores = [evaluate_agent(agent, env) for agent in population]

        # Logging e stampa risultati
        for i, score in enumerate(scores):
            print(f" Agent {i} score: {score:.2f}")
            monitor.log(gen * population_size + i, score, cost=0)

        # Selezione migliori agenti
        top_agents = select_top_agents(population, scores, top_k)

        # Creazione nuova popolazione per prossima generazione
        new_population = []
        while len(new_population) < population_size:
            parent1 = random.choice(top_agents)
            parent2 = random.choice(top_agents)
            # Crossover e mutazione genomi
            child_genome = parent1.genome.crossover(parent2.genome)
            child_genome.mutate(mutation_rate)
            # Crea nuovo agente da genoma figlio
            child_agent = create_genetic_defender(env, child_genome.genes)
            new_population.append(child_agent)

        population = new_population

        # Salvataggio checkpoint
        save_path = os.path.join(cfg["checkpoint_dir"], f"gen_{gen+1}.pth")
        save_population(population, save_path)

def save_population(population, path):
    """
    Salva la popolazione corrente serializzando i genomi in file con torch.
    - population: lista agenti (ogni agente deve avere .genome.genes)
    - path: percorso file di salvataggio
    """
    genomes = [agent.genome.genes for agent in population]
    torch.save(genomes, path)

def main():
    """
    Funzione entrypoint del trainer.
    Carica configurazione, limita risorse, crea monitor e avvia il training loop.
    """
    cfg = load_config("configs/safe_rl_config.yaml")

    # Crea cartella checkpoint se non esiste
    os.makedirs(cfg.get("checkpoint_dir", "checkpoints"), exist_ok=True)

    limit_resources(cfg["max_cpu"], cfg["max_ram_mb"])

    monitor = Monitor(cfg["log_dir"])
    train_loop(cfg, monitor)

if __name__ == "__main__":
    main()
