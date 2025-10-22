using Application.Dto;
using Domain.Model.Entities;
using Domain.Model.ValueObjects;

namespace Application.Mappers
{

    /*
    I mapper stanno nel layer Application, quindi:conoscono Domain (le entità e i value object);
    ma non conoscono Infrastructure (nessuna dipendenza verso DTO di persistenza).

    Le conversioni sono pulite, riutilizzabili e testabili.

    La console/presentation layer interagisce solo con gli Application.Dto — mai con le entità di dominio direttamente.

    La responsabilità del mapping è totalmente separata da AnimalService.
     */
    public static class AnimalMapper
    {
        //this AnimalDto dto --> ci fa capire che toEntity è
        //un metodo di estensione di AnimalDto
        public static Animal ToEntity(this AnimalDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // 1️ Creo l'Animal concreto senza visite
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

            // 2️ Creo le visite, associando l'Animal concreto
            //per ogni VeterinaryVisitDto presente in dto creo un oggetto VeterinaryVisit
            //e lo metto in una lista

            List<VeterinaryVisit> visitList = new List<VeterinaryVisit>();
            foreach(VeterinaryVisitDto v in dto.Visits)
            {
                VeterinaryVisit visit = v.ToEntity(animal);
                visitList.Add(visit);
            }
            
            // 3️ Aggiungo le visite all'Animal
            foreach (var visit in visitList)
            {
                animal.AddVisit(visit);
            }

            return animal;
        }

        ////this Animal animal --> ci fa capire che ToDto è
        //un metodo di estensione di Animal
        public static AnimalDto ToDto(this Animal animal)
        {
            List<VeterinaryVisitDto> visits = new List<VeterinaryVisitDto>();
            foreach( VeterinaryVisit v in animal.VisitList)
            {
                VeterinaryVisitDto visitDto = v.ToDto();
                visits.Add(visitDto);
            }
           

            return new AnimalDto(
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
    }
}
