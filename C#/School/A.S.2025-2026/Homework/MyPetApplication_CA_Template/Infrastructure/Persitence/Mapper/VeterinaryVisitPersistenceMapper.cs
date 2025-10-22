using Domain.Model.Entities;
using Domain.Model.ValueObjects;
using Infrastructure.Persitence.Dto;

namespace Infrastructure.Persistence.Mappers
{
    public static class VeterinaryVisitPersistenceMapper
    {
        public static VeterinaryVisitPersistenceDto ToPersistenceDto(this VeterinaryVisit visit)
        {
            return new VeterinaryVisitPersistenceDto(
                visit.Veterinary.FirstName,
                visit.Veterinary.LastName,
                visit.Veterinary.Email.Value,
                visit.Veterinary.Phone.Value,
                visit.Veterinary.Specialization,
                visit.Date,
                visit.Results.ToString(),
                visit.Notes
            );
        }

        public static VeterinaryVisit ToEntity(this VeterinaryVisitPersistenceDto dto, Animal animal)
        {
            return new VeterinaryVisit(
                animal,
                new Veterinary(
                    dto.VeterinaryFirstName,
                    dto.VeterinaryLastName,
                    new Email(dto.VeterinaryEmail),
                    new PhoneNumber(dto.VeterinaryPhone),
                    dto.VeterinarySpecialization
                ),
                dto.Date,
                Enum.Parse<VisitResults>(dto.Result),
                dto.Notes
            );
        }
    }
}
