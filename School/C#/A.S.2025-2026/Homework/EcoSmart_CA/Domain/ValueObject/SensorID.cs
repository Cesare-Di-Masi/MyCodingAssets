using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ValueObject
{
    public record SensorId
    {
        // Validazione: Un ID non può essere vuoto
        private string _value;
        public SensorId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("SensorId cannot be empty.", nameof(value));
            _value = value;
        }

        public string GetValue() => _value;
    }
}