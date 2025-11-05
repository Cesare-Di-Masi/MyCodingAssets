using Domain.Model.Entities;
using Domain.Model.ValueObjects;
using Infrastructure.Dto;

namespace Infrastructure.Mapper
{
    public static class CatPersistenceMapper
    {
        public static Cat ToEntity(this CatPersistenceDto dto)
        {
            return new Cat(
                dto.name,
                dto.isMale,
                dto.arrivingDate,
                dto.birthdate,
                new Breed(dto.breed),
                dto.description,
                dto.id
                );
        }

        public static CatPersistenceDto ToDto(this Cat entity)
        {
            return new CatPersistenceDto(
                entity.ID,
                entity.Name,
                entity.Breed.Name,
                entity.IsMale,
                entity.ArrivingDate,
                entity.BirthDate,
                entity.Description
                );
        }
    }
}