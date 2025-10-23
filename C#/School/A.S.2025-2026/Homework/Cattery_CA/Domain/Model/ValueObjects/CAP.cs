namespace Domain.Model.ValueObjects
{
    public record CAP
    {
        public string Value { get; }

        public CAP(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 5 || !value.All(char.IsDigit))
                throw new ArgumentException("CAP non valido deve essere di 5 cifre.");
            Value = value;
        }
    }
}