using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace CatteryManagerLib
{
    public class Adopter
    {
        private string _name, _surname;
        private string? _mail;
        private string? _phone;

        public string Name
        {
            get { return _name; }
            set
            {
                if (String.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Name cannot be null or empty.");
                }
                _name = value;
            }
        }

        public string Surname
        {
            get { return _surname; }
            set
            {
                if (String.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Surname cannot be null or empty.");
                }
                _surname = value;
            }
        }

        public string? Phone
        {
            get { return _phone; }
            set
            {
                if(value != null && value.Length < 5)
                {
                    throw new ArgumentException("Phone number must be at least 5 characters long.");
                }
                _phone = value;
            }
        }

        public string? Mail
        {
            get { return _mail; }
            set
            {
                _mail = value;
            }
        }

        public Adopter(string name, string surname, string? mail)
        {
            Name = name;
            Surname = surname;
            Mail = mail;
            Phone = null;
        }

        public Adopter(string name, string surname, string mail, string? phone)
        {
            Name = name;
            Surname = surname;
            Mail = mail;
            Phone = phone;
        }

        public override bool Equals(object? obj)
        {
            if(obj is Adopter)
                {
                Adopter other = (Adopter)obj;
                return this.Name == other.Name && this.Surname == other.Surname && this.Phone == other.Phone && ((this.Mail == null && other.Mail == null) || (this.Mail != null && other.Mail != null && this.Mail == other.Mail));
            }
            return false;
        }

        public override string ToString()
        {
            return $"{Name} {Surname} - Mail: {(Mail != null ? Mail : "N/A")} - Phone: {(Phone != null ? Phone : "N/A")}";
        }

    }
}
