using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Dto;
using Domain.Entity;
using Domain.ValueObject;
using Domain.Enum;

namespace Application.Mapper
{
    public static class MeasurementMapper
    {
        public static MeasurementDto ToDto(this Measurement measurement)
        {
            if (measurement.Type == SensorType.Temperature)
            {
                return new MeasurementDto
                (
                    measurement.SensorId,
                    measurement.Value,
                    measurement.Timestamp,
                    "Celsius"
                    );
            }
            else if (measurement.Type == SensorType.Humidity)
            {
                return new MeasurementDto
                (
                    measurement.SensorId,
                    measurement.Value,
                    measurement.Timestamp,
                    "%"
                );
            }
            else if (measurement.Type == SensorType.CO2)
            {
                return new MeasurementDto
                (
                    measurement.SensorId,
                    measurement.Value,
                    measurement.Timestamp,
                    "PPM"
                );
            }
            else
            {
                throw new ArgumentException("Unknown SensorType", nameof(measurement.Type));
            }
        }

        public static Measurement ToDomain(this MeasurementDto dto)
        {
            if (dto.Type == "Celsius")
            {
                return new Measurement
                (
                    dto.Value,
                    SensorType.Temperature,
                    dto.SensorId,
                    dto.Timestamp
                );
            }
            else if (dto.Type == "%")
            {
                return new Measurement
                (
                    dto.Value,
                    SensorType.Humidity,
                    dto.SensorId,
                    dto.Timestamp
                );
            }
            else if (dto.Type == "PPM")
            {
                return new Measurement
                (
                    dto.Value,
                    SensorType.CO2,
                    dto.SensorId,
                    dto.Timestamp
                );
            }
            else
            {
                throw new ArgumentException("Unknown measurement type", nameof(dto.Type));
            }
        }
    }
}