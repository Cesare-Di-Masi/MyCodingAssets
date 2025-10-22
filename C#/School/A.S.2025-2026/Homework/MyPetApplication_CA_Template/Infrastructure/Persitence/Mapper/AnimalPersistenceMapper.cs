using Domain.Model.Entities;
using Domain.Model.ValueObjects;
using Infrastructure.Persitence.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persitence.Mapper
{
    internal static class AnimalPersistenceMapper
    {
        // ===== Entity → DTO =====
        public static AnimalPersistenceDto ToPersistenceDto(this Animal animal)
        {
            var visits = animal.VisitList
                .Select(v => new VeterinaryVisitPersistenceDto(
                    v.Veterinary.FirstName,
                    v.Veterinary.LastName,
                    v.Veterinary.Email.Value,
                    v.Veterinary.Phone.Value,
                    v.Veterinary.Specialization,
                    v.Date,
                    v.Results.ToString(),  
                    v.Notes
                ))
                .ToList();

          

            return new AnimalPersistenceDto(
                Name: animal.Name,
                Breed: animal.Breed,
                Birthday: animal.Birthday?.Value.ToDateTime(TimeOnly.MinValue),
                FavouriteGame: animal.FavouriteGame,
                FavouriteFood: animal.FavouriteFood,
                FavouriteChewing: (animal is Dog d) ? d.FavouriteChewing : null,
                Type: animal is Dog ? "Dog" : "Cat",
                Visits: visits
            );
        }

        // ===== DTO → Entity =====
        public static Animal ToEntity(this AnimalPersistenceDto dto)
        {
            Animal animal = dto.Type switch
            {
                "Dog" => new Dog(
                    name: dto.Name,
                    birthdate: dto.Birthday != null ? new Birthdate(DateOnly.FromDateTime(dto.Birthday.Value)) : new Birthdate(DateOnly.FromDateTime(DateTime.Now)),
                    breed: dto.Breed ?? "Unknown",
                    favouriteChewing: dto.FavouriteChewing ?? string.Empty
                )
                {
                    FavouriteGame = dto.FavouriteGame,
                    FavouriteFood = dto.FavouriteFood
                },

                "Cat" => new Cat(
                    name: dto.Name,
                    birthdate: dto.Birthday != null ? new Birthdate(DateOnly.FromDateTime(dto.Birthday.Value)) : new Birthdate(DateOnly.FromDateTime(DateTime.Now)),
                    breed: dto.Breed ?? "Unknown"
                )
                {
                    FavouriteGame = dto.FavouriteGame,
                    FavouriteFood = dto.FavouriteFood
                },

                _ => throw new ArgumentException($"Unknown Animal Type: {dto.Type}")
            };

            var visits = dto.Visits?
                .Select(v => new VeterinaryVisit(
                    animal,
                    new Veterinary(
                        v.VeterinaryFirstName,
                        v.VeterinaryLastName,
                        new Email(v.VeterinaryEmail),
                        new PhoneNumber(v.VeterinaryPhone),
                        v.VeterinarySpecialization
                    ),
                    v.Date,
                    Enum.Parse<VisitResults>(v.Result),
                    v.Notes
                ))
                .ToList() ?? new List<VeterinaryVisit>();

            foreach (var visit in visits)
            {
                animal.AddVisit(visit);
            }

            return animal;
        }
    }
    

}
