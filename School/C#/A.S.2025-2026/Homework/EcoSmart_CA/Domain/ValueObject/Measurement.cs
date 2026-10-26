using Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ValueObject
{
    public record Measurement
    {
        public Measurement(double value, SensorType type, string sensorId, DateTime date)
        {
            if (value < 0)
                throw new ArgumentException("MeasurementValue cannot be negative.", nameof(value));
            Value = value;
            Type = type;
            Timestamp = date;
            SensorId = sensorId;
        }

        public DateTime Timestamp { get; init; }
        public string SensorId { get; init; }
        public SensorType Type { get; init; }

        public double Value { get; init; }
    }
}