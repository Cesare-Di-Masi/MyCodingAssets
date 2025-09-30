using Domain.ValueObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Veterinary
    {
        private Email _email;
        private PhoneNumber _phoneNumber;

        private string _name;
        private string _address;

        private List<VeterinaryVisit> _veterinaryVisits;

        public Email Mail
        {
            get { return _email; }
            private set { _email = value; }
        }

        public PhoneNumber PhoneNumber
        {
            get { return _phoneNumber; }
            private set { _phoneNumber = value; }
        }

        public string Name
        {
            get { return _name; }
            private set { _name = value; }
        }

        public string Address
        {
            get { return _address; }
            private set { _address = value; }
        }

        public List<VeterinaryVisit> VisitsRecord
        {
            get { return _veterinaryVisits; }
            private set { _veterinaryVisits = value; }
        }

        public Veterinary(Email email, PhoneNumber phoneNumber, string name, string address)
        {
            _email = email;
            _phoneNumber = phoneNumber;
            _name = name;
            _address = address;
            _veterinaryVisits = new List<VeterinaryVisit>();
        }

        public void addVisit(VeterinaryVisit visit)
        {
            _veterinaryVisits.Add(visit);
        }
    }
}