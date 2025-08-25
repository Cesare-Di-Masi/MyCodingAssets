using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatteryManagerLib
{
    public class Adoption
    {
        private Cat _cat;
        private Adopter _adopter;
        private DateOnly _adoptionDate;

        public Cat Cat
        {
            get { return _cat; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("Cat cannot be null.");
                }
                _cat = value;
            }
        }

        public Adopter Adopter
        {
            get { return _adopter; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("Adopter cannot be null.");
                }
                _adopter = value;
            }
        }

        public DateOnly AdoptionDate
            {
            get { return _adoptionDate; }
            set
            {
                _adoptionDate = value;
            }
        }

        public bool AdoptionActive
        {
            get { return Cat.ExitDate == null; }
        }

        public Adoption(Cat cat, Adopter adopter)
        {
            Cat = cat;
            Adopter = adopter;
            AdoptionDate = DateOnly.FromDateTime(DateTime.Now);
            Cat.GettingAdopted(AdoptionDate);
        }

        public void AdoptionCanceled()
        {
            Cat.AdoptionCanceled();
        }

        public override string ToString()
        {
            return $"{Adopter.Name} {Adopter.Surname} adopted {Cat.Name} on {AdoptionDate}";
        }

        public override bool Equals(object? obj)
        {
            if(obj is Adopter)
                {
                Adoption other = (Adoption)obj;
                return this.Cat.Equals(other.Cat) && this.Adopter.Equals(other.Adopter) && this.AdoptionDate.Equals(other.AdoptionDate);
            }
            return false;
        }


    }
}
