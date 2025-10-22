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
        public string name { get; }
        public string animal { get; }

        public List<Color> colors { get; }

        public Breed(string name, string animal, List<Color> colors = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("name cannot be null or whitespace", nameof(name));
            if (string.IsNullOrWhiteSpace(animal))
                throw new ArgumentException("animal cannot be null or whitespace", nameof(animal));
            if (colors == null || colors.Count == 0)
                colors = new List<Color>();
            this.name = name;
            this.animal = animal;
            this.colors = colors;
        }
    }
}