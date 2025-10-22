using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persitence.Dto
{
    /// <summary>
    /// dto interno a infrastructure per la gestione dell'animal
    /// </summary>
    /// <param name="Name"></param>
    /// <param name="Breed"></param>
    /// <param name="Birthday"></param>
    /// <param name="FavouriteGame"></param>
    /// <param name="FavouriteFood"></param>
    /// <param name="FavouriteChewing"></param>
    /// <param name="Type"></param>
    /// <param name="Visits"></param>
    internal record AnimalPersistenceDto(
    string Name,
    string? Breed,
    DateTime? Birthday,
    string? FavouriteGame,
    string? FavouriteFood,
    string? FavouriteChewing,
    string Type, // "Dog" o "Cat"
    List<VeterinaryVisitPersistenceDto>? Visits = null
);
}
