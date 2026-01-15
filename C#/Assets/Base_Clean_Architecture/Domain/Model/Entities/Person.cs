using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Domain.Model.ValueObjects;

namespace Domain.Model.Entities
{
    public abstract class Person
    {
        protected FullName _fullname;
        protected PhoneNumber _phoneNumber;
        protected DateTime _birthdate;
        protected int _age;
        protected TaxIDCode _taxIDCode;
        protected Email _email;
        protected int _residence;
        protected CAP _cap;
        protected int _gender;

        public FullName Fullname
        {
            get => default;
            set
            {
            }
        }

        public PhoneNumber PhoneNumber
        {
            get => default;
            set
            {
            }
        }

        public System.DateTime Birthdate
        {
            get => default;
            set
            {
            }
        }

        public int Age
        {
            get => default;
            set
            {
            }
        }

        public TaxIDCode TaxIDCode
        {
            get => default;
            set
            {
            }
        }

        public Email Email
        {
            get => default;
            set
            {
            }
        }

        public int Residence
        {
            get => default;
            set
            {
            }
        }

        public CAP CAP
        {
            get => default;
            set
            {
            }
        }

        public int Gender
        {
            get => default;
            set
            {
            }
        }

        public Person(FullName fullname, PhoneNumber phoneNumber, DateTime birthdate, int age, TaxIDCode taxIDCode, Email email, int residence, CAP cap, int gender)
        {
            _fullname = fullname;
            _phoneNumber = phoneNumber;
            _birthdate = birthdate;
            _age = age;
            _taxIDCode = taxIDCode;
            _email = email;
            _residence = residence;
            _cap = cap;
            _gender = gender;
        }
    }
}