namespace Infrastructure.Dto
{
    public record CatPersistenceDto
        (
            string id,
            string name,
            string breed,
            bool isMale,
            DateOnly arrivingDate,
            DateOnly? birthdate,
            string? description
            );
}