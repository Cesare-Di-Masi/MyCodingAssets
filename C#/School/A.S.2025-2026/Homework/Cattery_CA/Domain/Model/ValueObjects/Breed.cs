using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model.ValueObjects
{
    public record Breed
    {
        public string Name { get; }

        public List<Color> Colors { get; }

        public Breed(string name, List<Color> colors = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("name cannot be null or whitespace", nameof(name));
            if (colors == null || colors.Count == 0)
                colors = new List<Color>();
            this.Name = name;
            this.Colors = colors;
        }
    }
}