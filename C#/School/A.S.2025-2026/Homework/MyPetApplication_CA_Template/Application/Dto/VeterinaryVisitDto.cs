using Domain.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public record VeterinaryVisitDto(
        VeterinaryDto Veterinary,
        DateTime Date,
        VisitResults Result,
        string? Notes
    );

}
