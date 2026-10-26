namespace Application.Dto
{
    public record CatDto(
        string Name,
        bool IsMale,
        DateOnly ArrivingDate,
        DateOnly? BirthDate,
        string? Description,
        string? BreedName,
        string? Id
        );
}