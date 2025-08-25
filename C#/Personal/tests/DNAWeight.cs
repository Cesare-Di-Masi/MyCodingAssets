using System;
using System.Collections.Generic;
using Xunit;
using Newtonsoft.Json;


namespace WeaponDNA.Tests
{
    /// <summary>
    /// Sistema di pesi per influenzare la generazione casuale dei geni.
    /// Ogni gene o categoria può avere un peso per alterare la probabilità di selezione.
    /// </summary>
    public class GeneWeightSystem
    {
        private readonly Dictionary<string, double> _weights;
        private readonly Random _rng;

        public GeneWeightSystem(Dictionary<string, double> weights = null, Random rng = null)
        {
            _weights = weights ?? new Dictionary<string, double>();
            _rng = rng ?? new Random();
        }

        public void SetWeight(string gene, double weight)
        {
            _weights[gene] = weight;
        }

        public double GetWeight(string gene)
        {
            return _weights.ContainsKey(gene) ? _weights[gene] : 1.0;
        }

        /// <summary>
        /// Seleziona un valore da una lista applicando pesi se definiti.
        /// </summary>
        public string WeightedChoice(List<string> options)
        {
            double total = 0;
            foreach (var opt in options)
                total += GetWeight(opt);

            double roll = _rng.NextDouble() * total;
            double cumulative = 0;

            foreach (var opt in options)
            {
                cumulative += GetWeight(opt);
                if (roll <= cumulative)
                    return opt;
            }

            return options[^1]; // fallback
        }
    }

    public class WeaponDnaTests
    {
        [Fact]
        public void Test_Dna_Generation_ShouldProduce_ValidObject()
        {
            var dna = WeaponDna.GenerateRandom();
            Assert.NotNull(dna);
            Assert.False(string.IsNullOrEmpty(dna.Fingerprint));
        }

        [Fact]
        public void Test_Dna_Mutation_ShouldChangeGenes()
        {
            var dna = WeaponDna.GenerateRandom();
            var originalFingerprint = dna.Fingerprint;
            dna.Mutate();
            Assert.NotEqual(originalFingerprint, dna.Fingerprint);
        }

        [Fact]
        public void Test_Dna_Crossover_ShouldCombineParents()
        {
            var parent1 = WeaponDna.GenerateRandom();
            var parent2 = WeaponDna.GenerateRandom();
            var child = parent1.Crossover(parent2);

            Assert.NotNull(child);
            Assert.NotEqual(parent1.Fingerprint, child.Fingerprint);
            Assert.NotEqual(parent2.Fingerprint, child.Fingerprint);
        }

        [Fact]
        public void Test_WeightedChoice_ShouldRespectWeights()
        {
            var weights = new Dictionary<string, double>
            {
                { "sword", 10.0 },
                { "axe", 1.0 },
                { "spear", 1.0 }
            };

            var system = new GeneWeightSystem(weights, new Random(42));
            var options = new List<string> { "sword", "axe", "spear" };

            int swordCount = 0;
            for (int i = 0; i < 1000; i++)
            {
                var choice = system.WeightedChoice(options);
                if (choice == "sword") swordCount++;
            }

            Assert.True(swordCount > 600, $"Sword count too low: {swordCount}");
        }

        [Fact]
        public void Test_Dna_Validation_ShouldRepairInconsistencies()
        {
            var dna = WeaponDna.GenerateRandom();
            dna.Genes["delivery"] = "invalid";
            dna.ValidateAndRepair();
            Assert.Contains(dna.Genes["delivery"], new[] { "melee", "projectile", "beam", "explosive", "summon" });
        }
    }
}
