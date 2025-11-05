using Application.Dto;
using Domain.Model.Entities;
using Domain.Model.ValueObjects;

namespace Application.Mappers
{
    public static class CatMapper
    {
        public static Cat ToEntity(this CatDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new Cat(
                name: dto.Name,
                isMale: dto.IsMale,
                arrivingDate: dto.ArrivingDate,
                birthDate: dto.BirthDate,
                breed: dto.BreedName == null ? new Breed("Unknown") : new Breed(dto.BreedName),
                description: dto.Description,
                Id: dto.Id
            );
        }

        public static CatDto ToDto(this Cat entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            return new CatDto(
                Name: entity.Name,
                IsMale: entity.IsMale,
                ArrivingDate: entity.ArrivingDate,
                BirthDate: entity.BirthDate,
                Description: entity.Description,
                BreedName: entity.Breed?.Name,
                Id: entity.ID
            );
        }
    }
}