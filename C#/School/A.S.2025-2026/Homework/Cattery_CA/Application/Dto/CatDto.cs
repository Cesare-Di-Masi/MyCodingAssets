using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public record CatDto(
        string Name,
        bool IsMale,
        DateOnly ArrivingDate,
        DateOnly? BirthDate,
        string? Description,
        string? BreedName,
        string Id
        );
    
}
