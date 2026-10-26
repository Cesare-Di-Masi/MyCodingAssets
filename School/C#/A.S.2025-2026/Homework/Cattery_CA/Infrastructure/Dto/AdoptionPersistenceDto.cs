namespace Infrastructure.Dto
{
    public record AdoptionPersistenceDto
        (
            DateOnly adoptionDate,
            CatPersistenceDto cat,
            AdopterPersistenceDto adopterTax,
            string? description
            );
}