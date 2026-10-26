using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto
{
    public record MeasurementDto
    (
         string SensorId,
         double Value,
         DateTime Timestamp,
         string Type
    );
}