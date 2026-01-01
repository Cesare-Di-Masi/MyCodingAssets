using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Dto;
using Domain.ValueObject;
using Domain.Enum;
using Application.Mapper;
using Domain.Entity;

namespace Application.UseCases
{
    public class SensorService
    {
        private readonly ISensorRepository _sensorRepository;

        public SensorService(ISensorRepository sensorRepository)
        {
            _sensorRepository = sensorRepository;
        }

        public async Task<double> GetAverageTemperatureLast24hAsync()
        {
            var data = await _sensorRepository.GetAllDataAsync();
            var last24h = DateTime.Now.AddHours(-24);

            // Nota: Accesso alla proprietà .Amount del Value Object
            return data
                .Where(d => d.Type == SensorType.Temperature && d.Timestamp >= last24h)
                .Average(d => d.Value);
        }

        public async Task<List<MeasurementDto>> GetHighCO2SensorsAsync()
        {
            var data = await _sensorRepository.GetAllDataAsync();

            return data
                .Where(d => d.Type == SensorType.CO2 && d.Value > 1000)
                .OrderByDescending(d => d.Timestamp)
                .Select(d => d.ToDto())
                .ToList();
        }

        public async Task<List<MeasurementDto>> GetMaxHumidityPerSensorAsync()
        {
            var data = await _sensorRepository.GetAllDataAsync();

            return data
                .Where(d => d.Type == SensorType.Humidity)
                .GroupBy(d => d.SensorId)
                .Select(g => new MeasurementDto
                (
                     g.Key.ToString(),
                     g.Max(x => x.Value),
                     g.OrderByDescending(x => x.Timestamp).FirstOrDefault().Timestamp,
                     "%"
                ))
                .ToList();
        }
    }
}