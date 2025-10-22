using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public record AnimalDto(
        string Name,
        string? Breed,
        DateTime? Birthday,
        string? FavouriteGame,
        string? FavouriteFood,
        string? FavouriteChewing,
        string Type, // Dog o Cat
        List<VeterinaryVisitDto>? Visits = null
    );

}
