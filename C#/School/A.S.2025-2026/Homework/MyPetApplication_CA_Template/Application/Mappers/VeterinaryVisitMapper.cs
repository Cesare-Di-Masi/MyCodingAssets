using Application.Dto;
using Domain.Model.Entities;
using Domain.Model.ValueObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Mappers
{
    public static class VeterinaryVisitMapper
    {
        public static VeterinaryVisit ToEntity(this VeterinaryVisitDto dto, Animal animal)
        {
            return new VeterinaryVisit(
                animal,
                dto.Veterinary.ToEntity(),
                dto.Date,
                dto.Result,
                dto.Notes
            );
        }

        public static VeterinaryVisitDto ToDto(this VeterinaryVisit visit)
        {
            return new VeterinaryVisitDto(
                visit.Veterinary.ToDto(),
                visit.Date,
                visit.Results,
                visit.Notes
            );
        }
    }
}
