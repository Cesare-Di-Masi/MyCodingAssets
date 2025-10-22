using Application.Dto;
using Domain.Model.Entities;
using Domain.Model.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Mappers
{
    public static class VeterinaryMapper
    {
        public static Veterinary ToEntity(this VeterinaryDto dto)
        {
            return new Veterinary(
                dto.FirstName,
                dto.LastName,
                new Email(dto.Email),
                new PhoneNumber(dto.Phone),
                dto.Specialization
            );
        }

        public static VeterinaryDto ToDto(this Veterinary vet)
        {
            return new VeterinaryDto(
                vet.FirstName,
                vet.LastName,
                vet.Email.Value,
                vet.Phone.Value,
                vet.Specialization
            );
        }
    }

    
}
