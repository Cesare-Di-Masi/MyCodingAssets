using Domain.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persitence.Dto
{
    /// <summary>
    ///  dto interno a infrastructure per la gestione delle visite
    /// </summary>
    /// <param name="VeterinaryFirstName"></param>
    /// <param name="VeterinaryLastName"></param>
    /// <param name="VeterinaryEmail"></param>
    /// <param name="VeterinaryPhone"></param>
    /// <param name="VeterinarySpecialization"></param>
    /// <param name="Date"></param>
    /// <param name="Result"></param>
    /// <param name="Notes"></param>
    public record VeterinaryVisitPersistenceDto(
        string VeterinaryFirstName,
        string VeterinaryLastName,
        string VeterinaryEmail,
        string VeterinaryPhone,
        string VeterinarySpecialization,
        DateTime Date,
        string Result,
        string? Notes
    );
}
