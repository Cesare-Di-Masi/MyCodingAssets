namespace Application.Dto
{
    public record AdoptionDto(
        AdopterDto Adopter,
        CatDto Cat,
        DateOnly AdoptionDate,
        string? Description
        );
}