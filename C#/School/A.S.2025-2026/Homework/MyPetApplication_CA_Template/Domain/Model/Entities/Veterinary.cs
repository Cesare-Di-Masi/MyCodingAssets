using Domain.Model.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model.Entities
{
    public class Veterinary
    {
        public String FirstName { get; private set; }
        public String LastName { get; private set; }
        public Email Email { get; private set; }
        public PhoneNumber Phone { get; private set; }
        public string Specialization { get; private set; }

        public Veterinary(String firstName, string lastName, Email email, PhoneNumber phone, string specialization = null)
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            Phone = phone;
            Specialization = string.IsNullOrWhiteSpace(specialization) ? "General" : specialization;
            /*
            if (string.IsNullOrWhiteSpace(specialization))
                Specialization = "General";
            else
                Specialization = specialization;
            */
        }

        public Veterinary(string name, string surname, string email, string phone, string specialization = null)
            : this(name, surname, new Email(email), new PhoneNumber(phone), specialization) { }

        public override string ToString() => $"{LastName} {FirstName} ({Specialization}) - {Email}";

        /// <summary>
        /// consideriamo uguali due veterinari se hanno lo stesso fullname
        /// (non sarebbe un identificativo efficace perchè posso esistere
        /// due veterinari con lo stesso nome e cognome, ma per questo progetto può andar bene)
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object? obj)
        {
            if (obj == null || obj is not Veterinary) return false;
            Veterinary other = obj as Veterinary;

            //non devo sovrascrivere Equals di Fullname perchè essendo di tipo Fullname ed essendo
            //un record questi saranno uguali quando hanno valori uguali (e non in base all'indirizzo di memoria)
            return FirstName.Equals(other.FirstName) && LastName.Equals(other.LastName);
        }

        public override int GetHashCode()
        {
            return (FirstName + LastName).GetHashCode();
        }
    }
}