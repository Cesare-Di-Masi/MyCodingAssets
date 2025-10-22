namespace Domain.Model.Entities
{
    public class Adoption
    {
        public DateOnly AdoptionDate { get; }
        public Cat Cat { get; }
        public Adopter Adopter { get; }

        public string Description { get; private set; }

        public Adoption(DateOnly adoptionDate, Cat cat, Adopter adopter, string? description = null)
        {
            if(adoptionDate > DateOnly.FromDateTime(DateTime.Now))
                throw new ArgumentException("Adoption date cannot be in the future.", nameof(adoptionDate));
            AdoptionDate = adoptionDate;
            Cat = cat;
            Adopter = adopter;
            Description = description ?? "no description";
        }

        public void ModifyDescription(string description)
        {
            Description = description;
        }

        public override string ToString()
        {
            return $"Adoption of {Cat.Name} by {Adopter.FullName} on {AdoptionDate}. Description: {Description}";
        }

    }
}