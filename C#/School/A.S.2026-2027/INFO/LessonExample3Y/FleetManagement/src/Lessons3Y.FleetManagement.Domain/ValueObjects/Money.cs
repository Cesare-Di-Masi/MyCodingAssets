using System;
using System.Collections.Generic;
using System.Text;

namespace Lessons3Y.FleetManagement.Domain.ValueObjects
{
    /*
     * sealed --> The sealed modifier is used to prevent a class from being inherited. 
     * In this case, the Money record is sealed, meaning that no other class can derive from it. 
     * This is often done for security, performance, or design reasons, ensuring that the behavior of the Money record remains consistent and cannot be altered through inheritance.
     * 
     * Al momento possiamo anche ometterlo!
     */

    /// <summary>
    /// Represents a monetary value with an amount and currency.
    /// </summary>
    public sealed record Money
    {
        /// <summary>
        /// Gets the amount of money.
        /// </summary>
        public decimal Amount { get; }
        /// <summary>
        /// Gets the currency.
        /// </summary>
        public string Currency { get; }

        /// <summary>
        /// Initializes a new instance of the Money class with the specified amount and currency.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="currency"></param>
        /// <exception cref="ArgumentException"></exception>
        public Money(decimal amount, string currency = "EUR")
        {
            if (amount < 0)
                throw new ArgumentException("L'importo non può essere negativo.", nameof(amount));

            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("La valuta è obbligatoria.", nameof(currency));

            Amount = amount;
            Currency = currency.Trim().ToUpperInvariant();
        }
    }
}
