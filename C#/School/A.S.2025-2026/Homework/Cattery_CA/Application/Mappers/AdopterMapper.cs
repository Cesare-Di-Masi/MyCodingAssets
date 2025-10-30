using Application.Dto;
using Domain.Model.Entities;
using Domain.Model.ValueObjects;

namespace Application.Mappers
{
    public static class AdopterMapper
    {
        public static Adopter ToEntity(this AdopterDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new Adopter(
                fullName: new Domain.Model.ValueObjects.FullName(dto.FirstName, dto.LastName),
                birthDate: dto.BirthDate,
                taxIdCode: new TaxIDCode(dto.TaxIDCode),
                cap: new CAP(dto.CAP),
                email: new Domain.Model.ValueObjects.Email(dto.Email),
                phoneNumber: string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : new Domain.Model.ValueObjects.PhoneNumber(dto.PhoneNumber)
                );
        }

        public static AdopterDto ToDto(this Adopter entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            return new AdopterDto(
                entity.FullName.First,
                entity.FullName.Last,
                entity.TaxIDCode.Value,
                entity.CAP.Value,
                entity.PhoneNumber?.Value ?? string.Empty,
                entity.Email.Value,
                entity.BirthDate
                );
        }
    }
}