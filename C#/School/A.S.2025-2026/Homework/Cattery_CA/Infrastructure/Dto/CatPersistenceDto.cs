using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Dto
{
    public record CatPersistenceDto
        (
            string id,
            string name,
            string breed,
            bool isMale,
            DateOnly arrivingDate,
            DateOnly? birthdate,
            string? description
            );

}
