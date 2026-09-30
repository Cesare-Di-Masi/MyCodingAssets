using System;
using System.Collections.Generic;
using System.Text;

namespace Lessons3Y.FleetManagement.Domain.ValueObjects
{
    /// <summary>
    /// Represents a vehicle's license plate as a value object, ensuring proper formatting and validation.
    /// </summary>
    public sealed record LicensePlate
    {
        /// <summary>
        /// Gets the value of the license plate.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Initializes a new instance of the LicensePlate class with the specified value, performing validation on the input.
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException"></exception>
        public LicensePlate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("La targa non può essere vuota.", nameof(value));

            string cleanValue = value.Trim().ToUpperInvariant();

            if (cleanValue.Length < 6 || cleanValue.Length > 8)
                throw new ArgumentException("Il formato della targa non è valido.", nameof(value));

            Value = cleanValue;
        }

        public override string ToString()
        {
            return Value;
        }
    }

    
}
