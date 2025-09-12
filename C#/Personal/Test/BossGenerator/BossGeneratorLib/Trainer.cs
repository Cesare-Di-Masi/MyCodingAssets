using BossGeneratorLib.BossGeneratorLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BossGeneratorLib
{
    public class TrainingConfig
    {
        public int PopulationSize { get; set; } = 100;
        public int EliteSize { get; set; } = 10;
        public int Generations { get; set; } = 50;
        public double MutationRate { get; set; } = 0.15;
        public double RulesetBreakChance { get; set; } = 0.01;
        public int FinalBossCount { get; set; } = 5;
        public bool BattleEvaluation { get; set; } = true;
        public int BattleCount { get; set; } = 5;
        public double BattleWeight { get; set; } = 0.7;

        public Dictionary<string, (int Min, int Max)> AttributeLimits { get; set; }
        public Dictionary<string, (int Min, int Max)> WeaponLimits { get; set; }
        public Dictionary<string, (int Min, int Max)> SkillLimits { get; set; }
        public Dictionary<string, int> TargetAttributes { get; set; }
        public Dictionary<string, float> TargetBehaviors { get; set; }
    }

    public class TrainingResult
    {
        public List<Boss> FinalBosses { get; set; }
        public List<GenerationStats> GenerationStats { get; set; }
        public TrainingConfig Config { get; set; }
    }

    public class GenerationStats
    {
        public int Generation { get; set; }
        public double AverageFitness { get; set; }
        public double MaxFitness { get; set; }
        public double MinFitness { get; set; }
        public int PopulationSize { get; set; }
        public Boss BestBoss { get; set; }
        public double AverageHp { get; set; }
        public double AverageStrength { get; set; }
        public double AverageIntelligence { get; set; }
        public double AverageDefense { get; set; }
        public double AverageSpeed { get; set; }
    }

    public class Trainer
    {
        private readonly TrainRuler _trainRuler;
        private readonly BattleSystem _battleSystem;
        private readonly Random _random = new Random();

        public Trainer(List<StatusEffect> availableStatusEffects = null)
        {
            _trainRuler = new TrainRuler();
            _battleSystem = new BattleSystem();

            if (availableStatusEffects != null)
            {
                _trainRuler.AvailableStatusEffects = availableStatusEffects;
            }
            else
            {
                _trainRuler.AvailableStatusEffects = GetDefaultStatusEffects();
            }
        }

        public TrainingResult TrainBosses(TrainingConfig config = null)
        {
            if (config == null)
            {
                config = new TrainingConfig();
            }

            ApplyConfiguration(config);

            var population = GenerateInitialPopulation(config.PopulationSize);
            var bestBosses = new List<Boss>();
            var generationStats = new List<GenerationStats>();

            for (int gen = 0; gen < config.Generations; gen++)
            {
                var fitnessScores = EvaluatePopulation(population, config);
                var stats = CalculateGenerationStats(population, fitnessScores, gen);
                generationStats.Add(stats);

                var elite = SelectElite(population, fitnessScores, config.EliteSize);
                bestBosses.AddRange(elite);

                population = GenerateNextGeneration(elite, config);
                ApplyMutations(population, config);
                ApplyRulesetBreaking(population, config);
            }

            var finalBosses = SelectFinalBosses(bestBosses, config.FinalBossCount);

            return new TrainingResult
            {
                FinalBosses = finalBosses,
                GenerationStats = generationStats,
                Config = config
            };
        }

        private void ApplyConfiguration(TrainingConfig config)
        {
            _trainRuler.Starter.InitialPopulationSize = config.PopulationSize;
            _trainRuler.Starter.EliteSize = config.EliteSize;
            _trainRuler.Starter.Generations = config.Generations;
            _trainRuler.Starter.MutationRate = config.MutationRate;
            _trainRuler.Starter.RulesetBreakChance = config.RulesetBreakChance;

            if (config.AttributeLimits != null)
            {
                _trainRuler.Starter.AttributeLimits = config.AttributeLimits;
            }

            if (config.WeaponLimits != null)
            {
                _trainRuler.Starter.WeaponLimits = config.WeaponLimits;
            }

            if (config.SkillLimits != null)
            {
                _trainRuler.Starter.SkillLimits = config.SkillLimits;
            }

            if (config.TargetAttributes != null)
            {
                _trainRuler.Ender.TargetAttributes = config.TargetAttributes;
            }

            if (config.TargetBehaviors != null)
            {
                _trainRuler.Ender.TargetBehaviors = config.TargetBehaviors;
            }
        }

        private List<Boss> GenerateInitialPopulation(int populationSize)
        {
            var population = new List<Boss>();

            for (int i = 0; i < populationSize; i++)
            {
                var boss = AssetsGenerator.GenerateBoss(
                    _trainRuler.Starter,
                    _random,
                    _trainRuler.AvailableStatusEffects);
                population.Add(boss);
            }

            return population;
        }

        private List<double> EvaluatePopulation(List<Boss> population, TrainingConfig config)
        {
            var fitnessScores = new List<double>();

            foreach (var boss in population)
            {
                double fitness = 0;

                if (config.BattleEvaluation)
                {
                    double battleScore = EvaluateThroughBattles(boss, population, config.BattleCount);
                    fitness += battleScore * config.BattleWeight;
                }

                double fitnessScore = _trainRuler.CalculateFitness(boss);
                fitness += fitnessScore * (1 - config.BattleWeight);

                fitnessScores.Add(fitness);
            }

            return fitnessScores;
        }

        private double EvaluateThroughBattles(Boss boss, List<Boss> population, int battleCount)
        {
            double totalScore = 0;
            int wins = 0;
            double totalDamageDealt = 0;
            double totalDamageTaken = 0;
            double totalSurvivalTime = 0;
            double totalResourceEfficiency = 0;

            var opponents = population
                .Where(b => b != boss)
                .OrderBy(b => _random.Next())
                .Take(battleCount)
                .ToList();

            foreach (var opponent in opponents)
            {
                var result = _battleSystem.SimulateBattle(boss, opponent);

                if (result.Winner == boss)
                {
                    wins++;
                }

                totalDamageDealt += result.DamageDealt;
                totalDamageTaken += result.DamageTaken;
                totalSurvivalTime += result.SurvivalTime;
                totalResourceEfficiency += result.ResourceEfficiency;
            }

            double winRate = wins / (double)battleCount;
            double damageRatio = totalDamageDealt / Math.Max(1, totalDamageTaken);
            double avgSurvivalTime = totalSurvivalTime / battleCount;
            double avgResourceEfficiency = totalResourceEfficiency / battleCount;

            return (winRate * 0.4 +
                   Math.Min(1.0, damageRatio / 2.0) * 0.3 +
                   Math.Min(1.0, avgSurvivalTime / 50.0) * 0.15 +
                   avgResourceEfficiency * 0.15);
        }

        private GenerationStats CalculateGenerationStats(List<Boss> population, List<double> fitnessScores, int generation)
        {
            var stats = new GenerationStats
            {
                Generation = generation,
                AverageFitness = fitnessScores.Average(),
                MaxFitness = fitnessScores.Max(),
                MinFitness = fitnessScores.Min(),
                PopulationSize = population.Count
            };

            var bestBoss = population[fitnessScores.IndexOf(fitnessScores.Max())];
            stats.BestBoss = bestBoss;

            // Calcola le medie con gestione valori anomali
            stats.AverageHp = population.Average(b => b.Hp);
            stats.AverageStrength = population.Average(b => b.Strength);
            stats.AverageIntelligence = population.Average(b => b.Intelligence);
            stats.AverageDefense = population.Average(b => b.Defence);
            stats.AverageSpeed = population.Average(b => b.Speed);

            return stats;
        }

        private List<Boss> SelectElite(List<Boss> population, List<double> fitnessScores, int eliteSize)
        {
            var ranked = population.Zip(fitnessScores, (boss, fitness) =>
                new { Boss = boss, Fitness = fitness })
                .OrderByDescending(x => x.Fitness)
                .ToList();

            return ranked.Take(eliteSize).Select(x => x.Boss).ToList();
        }

        private List<Boss> GenerateNextGeneration(List<Boss> elite, TrainingConfig config)
        {
            var newGeneration = new List<Boss>();

            newGeneration.AddRange(elite.Select(boss => new Boss(boss)));

            while (newGeneration.Count < config.PopulationSize)
            {
                var parent1 = SelectParent(elite);
                var parent2 = SelectParent(elite);

                var child = Crossover(parent1, parent2);
                newGeneration.Add(child);
            }

            return newGeneration;
        }

        private Boss SelectParent(List<Boss> elite)
        {
            int tournamentSize = Math.Max(2, elite.Count / 4);
            var tournament = new List<Boss>();

            for (int i = 0; i < tournamentSize; i++)
            {
                tournament.Add(elite[_random.Next(elite.Count)]);
            }

            return tournament.OrderByDescending(boss =>
                _trainRuler.CalculateFitness(boss)).First();
        }

        private Boss Crossover(Boss parent1, Boss parent2)
        {
            var child = new Boss(parent1);

            if (_random.NextDouble() < 0.5)
            {
                child.Hp = parent2.Hp;
                child.Mana = parent2.Mana;
                child.Stamina = parent2.Stamina;
            }

            // Crossover AiPattern - usa i metodi sicuri
            foreach (var key in Boss.DefaultAiPatternKeys)
            {
                if (_random.NextDouble() < 0.5)
                {
                    child.SetAiBehavior(key, parent2.GetAiBehavior(key));
                }
            }

            child.CurrentWeapons = _random.NextDouble() < 0.5 ?
                new List<Weapon?>(parent1.CurrentWeapons) :
                new List<Weapon?>(parent2.CurrentWeapons);

            child.CurrentSkills = _random.NextDouble() < 0.5 ?
                new List<Skill?>(parent1.CurrentSkills) :
                new List<Skill?>(parent2.CurrentSkills);

            return child;
        }

        private void ApplyMutations(List<Boss> population, TrainingConfig config)
        {
            var mutator = new TrainRuler.Mutator(_trainRuler.Starter, _random);

            foreach (var boss in population)
            {
                if (_random.NextDouble() < config.MutationRate)
                {
                    mutator.MutateBoss(boss);
                }
            }
        }

        private void ApplyRulesetBreaking(List<Boss> population, TrainingConfig config)
        {
            foreach (var boss in population)
            {
                if (_random.NextDouble() < config.RulesetBreakChance)
                {
                    var attributes = new[] { "Strength", "Intelligence", "Defence", "Speed" };
                    var selectedAttr = attributes[_random.Next(attributes.Length)];

                    var currentValue = (int)typeof(Boss).GetProperty(selectedAttr).GetValue(boss);
                    var limits = _trainRuler.Starter.AttributeLimits[selectedAttr];

                    var breaker = (int)(limits.Max * (0.2 + _random.NextDouble() * 0.3));
                    typeof(Boss).GetProperty(selectedAttr).SetValue(boss, currentValue + breaker);
                }
            }
        }

        private List<Boss> SelectFinalBosses(List<Boss> allBestBosses, int count)
        {
            var scoredBosses = allBestBosses
                .Select(boss => new { Boss = boss, Fitness = _trainRuler.CalculateFitness(boss) })
                .OrderByDescending(x => x.Fitness)
                .ToList();

            return scoredBosses.Take(count).Select(x => x.Boss).ToList();
        }

        private List<StatusEffect> GetDefaultStatusEffects()
        {
            return new List<StatusEffect>
            {
                new StatusEffect("Poison", false, false, 5, 1.0f, 10, 1, 0.3f, 0,
                    new Dictionary<string, string>(), new List<StatusEffect>()),
                new StatusEffect("Burn", false, false, 8, 1.2f, 8, 1, 0.25f, 0,
                    new Dictionary<string, string>(), new List<StatusEffect>()),
                new StatusEffect("Freeze", false, false, 3, 0.8f, 5, 2, 0.2f, 2,
                    new Dictionary<string, string>(), new List<StatusEffect>()),
                new StatusEffect("Regen", true, false, 10, 1.0f, 5, 1, 0.4f, 0,
                    new Dictionary<string, string>(), new List<StatusEffect>()),
                new StatusEffect("DefenseUp", true, true, 15, 1.5f, 10, 0, 0.5f, 1,
                    new Dictionary<string, string>(), new List<StatusEffect>())
            };
        }
    }
}