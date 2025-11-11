using Domain.Model.Entities;
using Domain.Model.ValueObjects;
using Infrastructure.Dto;

namespace Infrastructure.Mapper
{
    public static class AdopterPersistenceMapper
    {
        public static Adopter ToEntity(this AdopterPersistenceDto dto)
        {
            return new Adopter(
                new FullName(dto.firstName, dto.lastName),
                dto.birthDate,
                new TaxIDCode(dto.taxIDCode),
                new CAP(dto.cap),
                new Email(dto.email),
                new PhoneNumber(dto.phoneNumber)
                );
        }

        public static AdopterPersistenceDto ToDto(this Adopter entity)
        {
            return new AdopterPersistenceDto(
                entity.FullName.First,
                entity.FullName.Last,
                entity.TaxIDCode.Value,
                entity.CAP.Value,
                entity.PhoneNumber != null ? entity.PhoneNumber.Value : string.Empty,
                entity.Email.Value,
                entity.BirthDate
                );
        }
    }
}