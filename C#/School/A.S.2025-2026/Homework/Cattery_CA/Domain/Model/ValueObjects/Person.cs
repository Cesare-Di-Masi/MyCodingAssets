using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model.ValueObjects
{
    public record Person
    {
        public FullName FullName { get; }
        public DateOnly BirthDate { get; }
        public Email Email { get; }
        public PhoneNumber? PhoneNumber { get; }

        public Person(FullName fullName,string surname, DateOnly birthDate, Email email, PhoneNumber? phoneNumber = null)
        {
            if (string.IsNullOrWhiteSpace(surname))
                throw new ArgumentException("Surname cannot be null or empty.", nameof(surname));

            if (birthDate > DateOnly.FromDateTime(DateTime.Now))
                throw new ArgumentException("Birth date cannot be in the future.", nameof(birthDate));

            FullName = fullName;
            BirthDate = birthDate;
            Email = email;
            PhoneNumber = phoneNumber;
        }
    }
}
