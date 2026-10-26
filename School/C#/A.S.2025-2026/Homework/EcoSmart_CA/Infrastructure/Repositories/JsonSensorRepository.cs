using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Application.Interfaces;
using Domain.Entity;
using Domain.ValueObject;
using Infrastructure.Dto;
using Infrastructure.MapperDto;

namespace Infrastructure.Repositories
{
    public class JsonSensorRepository : ISensorRepository
    {
        private readonly string _filePath = "sensors.json";
        private readonly Dictionary<string, Measurement> _cache = new();
        private bool _initialized = false;

        private async Task EnsureLoaded()
        {
            if (_initialized) return;

            if (!File.Exists(_filePath))
            {
                _initialized = true;
                return;
            }

            var json = await File.ReadAllTextAsync(_filePath);
            var sensorDataDtos = JsonSerializer.Deserialize<List<MeasurementJsonDto>>(json);

            foreach (var dto in sensorDataDtos)
            {
                _cache[dto.SensorId] = dto.ToEntity();
            }

            _initialized = true;
            return;
        }

        public async Task<List<Measurement>> GetAllDataAsync()
        {
            await EnsureLoaded();
            return _cache.Values.ToList();
        }
    }
}