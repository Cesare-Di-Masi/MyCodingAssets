namespace Infrastructure.Dto
{
    public record AdopterPersistenceDto
        (
            string firstName,
            string lastName,
            string taxIDCode,
            string cap,
            string phoneNumber,
            string email,
            DateOnly birthDate
            );
}