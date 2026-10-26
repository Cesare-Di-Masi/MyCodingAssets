using Domain.Model.Entities;
using Infrastructure.Dto;

namespace Infrastructure.Mapper
{
    public static class AdoptionPersistenceMapper
    {
        public static Adoption ToEntity(this AdoptionPersistenceDto dto)
        {
            return new Adoption(
                dto.adoptionDate,
                dto.cat.ToEntity(),
                dto.adopterTax.ToEntity(),
                dto.description
                );
        }

        public static AdoptionPersistenceDto ToDto(this Adoption entity)
        {
            return new AdoptionPersistenceDto
            (
                adoptionDate: entity.AdoptionDate,
                cat: entity.Cat.ToDto(),
                adopterTax: entity.Adopter.ToDto(),
                description: entity.Description
            );
        }
    }
}