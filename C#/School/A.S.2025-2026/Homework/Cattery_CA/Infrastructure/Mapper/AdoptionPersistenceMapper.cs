using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Model.Entities;
using Infrastructure.Dto;
using Domain.Model.ValueObjects;

namespace Infrastructure.Mapper
{
    public static class AdoptionPersistenceMapper
    {

        public static Adoption ToEntity(this AdoptionPersistenceDto dto)
        {
            return new Adoption(
                dto.adoptionDate,
                dto.cat.ToEntity(),
                dto.adopterTax.ToEntity(),
                dto.description
                );
        }

    }
}
