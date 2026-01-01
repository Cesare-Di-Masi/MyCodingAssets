using Domain.Enum;
using Domain.ValueObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entity
{
    public class SensorData
    {
        public SensorId SensorId { get; set; }
        public SensorType Type { get; set; }
        public Measurement Value { get; set; }
        public DateTime Timestamp { get; set; }

        // Costruttore vuoto necessario per la deserializzazione o il mapping manuale
        public SensorData()
        { }

        public SensorData(SensorId sensorId, SensorType type, Measurement value, DateTime timestamp)
        {
            SensorId = sensorId;
            Type = type;
            Value = value;
            Timestamp = timestamp;
        }
    }
}