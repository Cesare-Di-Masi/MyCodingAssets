namespace Domain.Model.ValueObjects
{
    public record TaxIDCode
    {
        public string Value { get; }

        public TaxIDCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 16 || !value.All(char.IsLetterOrDigit))
                throw new ArgumentException("Codice Fiscale non valido deve essere di 16 caratteri alfanumerici.");
            Value = value;
        }
    }
}