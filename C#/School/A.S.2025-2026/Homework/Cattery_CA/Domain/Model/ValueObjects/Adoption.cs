using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model.ValueObjects
{
    public record Adoption
    {
        public DateOnly AdoptionDate { get; }
        public Cat Cat { get; }
        public Person Adopter { get; }


        public Adoption(DateOnly adoptionDate, Cat cat, Person adopter)
        {
            AdoptionDate = adoptionDate;
            Cat = cat;
            Adopter = adopter;
        }

    }
}
