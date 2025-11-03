using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Dto
{
    public record AdopterPersistenceDto
        (
            string firstName,
            string lastName,
            string taxIDCode,
            string cap,
            string phoneNumber,
            string email,
            DateOnly birthDate
            );

}
