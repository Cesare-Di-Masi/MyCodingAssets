namespace Domain.Model.ValueObjects
{
    public record PhoneNumber
    {
        public string Value { get; }

        public PhoneNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Phone number not valid");

            if (!value.All(char.IsDigit) || value.Length < 7 || value.Length > 15) //atenzione, non c'è il controllo della lunghezza massima
                throw new ArgumentException("Invalid phone number format");

            Value = value;
        }

        public override string ToString() => Value;
    }
}