using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model.ValueObjects
{
    public record Cat
    {

        public string Name { get; }
        public bool IsMale { get; }
        public DateOnly ArrivingDate { get; }
        public DateOnly? BirthDate { get; }
        public Breed Breed { get; }
        public string? Description { get; }


        public Cat(string name, bool isMale, DateOnly arrivingDate, DateOnly? birthDate, Breed? breed, string? description)
        {
            Name = name;
            IsMale = isMale;
            ArrivingDate = arrivingDate;
            BirthDate = birthDate;
            Breed = breed ?? new Breed("no breed");
            Description = description ?? "no description";
        }

        public override string ToString()
        {
            return $"{Name} is a {(IsMale ? "male" : "female")} cat, arrived on {ArrivingDate}.";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, IsMale, ArrivingDate, BirthDate, Breed, Description);
        }

    }
}
