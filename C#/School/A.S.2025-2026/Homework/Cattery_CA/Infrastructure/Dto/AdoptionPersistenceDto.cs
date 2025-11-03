using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Dto
{
    public record AdoptionPersistenceDto
        (
            DateOnly adoptionDate,
            CatPersistenceDto cat,
            AdopterPersistenceDto adopterTax,
            string? description
            );
}
