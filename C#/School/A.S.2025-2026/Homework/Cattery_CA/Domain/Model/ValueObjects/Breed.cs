namespace Domain.Model.ValueObjects
{
    public record Breed
    {
        public string Name { get; }

        public string Description { get; }

        public Breed(string name, string? description = null)
        {
            this.Description = description;
            if (string.IsNullOrWhiteSpace(name))
                name = "Unknown";
            if (description == null || string.IsNullOrWhiteSpace(description))
                Description = "no description";
            this.Name = name;
        }
    }
}