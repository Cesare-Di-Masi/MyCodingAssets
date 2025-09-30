using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class VeterinaryVisit
    {
        private double _cost;

        public double Cost
        {
            get { return _cost; }
            private set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("illegal price");
                _cost = value;
            }
        }

        private DateTime _visitDate;

        public DateTime VisitDate
        {
            get { return _visitDate; }
            private set
            {
                if (value > DateTime.Now)
                    throw new ArgumentOutOfRangeException("illegal date");
                _visitDate = value;
            }
        }

        private string _description;

        public string Description
        {
            get { return _description; }
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("description cannot be null or empty");
                _description = value;
            }
        }

        private Animal _animal;

        public Animal Animal
        {
            get { return _animal; }
            private set
            {
                if (value == null)
                    throw new ArgumentNullException("animal cannot be null");
                _animal = value;
            }
        }

        public VeterinaryVisit(double cost, DateTime visitDate, string description, Animal animal)
        {
            Cost = cost;
            VisitDate = visitDate;
            Description = description;
            Animal = animal;
        }
    }
}