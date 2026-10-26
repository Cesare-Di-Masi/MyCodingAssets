using Application.Dto;
using Domain.Model.Entities;

namespace Application.Mappers
{
    public static class AdoptionMapper
    {
        public static Adoption ToEntity(this AdoptionDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            var cat = CatMapper.ToEntity(dto.Cat);
            var adopter = AdopterMapper.ToEntity(dto.Adopter);
            return new Adoption(
               adoptionDate: dto.AdoptionDate,
                cat: dto.Cat.ToEntity(),
                adopter: dto.Adopter.ToEntity(),
                description: dto.Description
            );
        }

        public static AdoptionDto ToDto(this Adoption entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            return new AdoptionDto(
                AdoptionDate: entity.AdoptionDate,
                Cat: entity.Cat.ToDto(),
                Adopter: entity.Adopter.ToDto(),
                Description: entity.Description
            );
        }
    }
}