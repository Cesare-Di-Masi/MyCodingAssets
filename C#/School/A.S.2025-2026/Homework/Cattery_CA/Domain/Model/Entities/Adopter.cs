using Domain.Model.ValueObjects;

namespace Domain.Model.Entities
{
    public class Adopter
    {
        public FullName FullName { get; }
        public DateOnly BirthDate { get; }
        public Email Email { get; }
        public PhoneNumber? PhoneNumber { get; }
        public TaxIDCode TaxIDCode { get; }
        public CAP CAP { get; }

        public Adopter(FullName fullName, DateOnly birthDate, TaxIDCode taxIdCode, CAP cap, Email email, PhoneNumber? phoneNumber = null)
        {
            if (birthDate > DateOnly.FromDateTime(DateTime.Now))
                throw new ArgumentException("Birth date cannot be in the future.", nameof(birthDate));

            FullName = fullName;
            BirthDate = birthDate;
            Email = email;
            PhoneNumber = phoneNumber;
            TaxIDCode = taxIdCode;
            CAP = cap;
        }

        public override string ToString()
        {
            return $"{FullName}, born on {BirthDate}, Email: {Email}" + (PhoneNumber != null ? $", Phone: {PhoneNumber}" : "");
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is Adopter))
                return false;

            Adopter other = (Adopter)obj;

            return FullName.Equals(other.FullName) &&
                   BirthDate.Equals(other.BirthDate) &&
                   Email.Equals(other.Email) &&
                   ((PhoneNumber == null && other.PhoneNumber == null) ||
                    (PhoneNumber != null && PhoneNumber.Equals(other.PhoneNumber)));
        }
    }
}