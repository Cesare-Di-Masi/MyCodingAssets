namespace Application.Dto
{
    public record AdopterDto(
        string FirstName,
        string LastName,
        string TaxIDCode,
        string CAP,
        string PhoneNumber,
        string Email,
        DateOnly BirthDate
        );
}