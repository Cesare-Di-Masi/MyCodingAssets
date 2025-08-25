namespace WeaponGenerator
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
}