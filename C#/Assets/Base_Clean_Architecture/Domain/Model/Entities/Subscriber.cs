using Domain.Model.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Domain.Model.Entities
{
    public class Subscriber : Person
    {
        public Subscriber(FullName fullname, PhoneNumber phoneNumber, DateTime birthdate, int age, TaxIDCode taxIDCode, Email email, int residence, CAP cap, int gender) : base(fullname, phoneNumber, birthdate, age, taxIDCode, email, residence, cap, gender)
        {
        }
    }
}