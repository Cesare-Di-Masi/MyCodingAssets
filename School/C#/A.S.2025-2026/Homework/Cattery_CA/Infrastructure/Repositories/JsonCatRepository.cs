using Application.Interfaces;
using Domain.Model.Entities;
using Infrastructure.Dto;
using Infrastructure.Mapper;

namespace Infrastructure.Repositories
{
    public class JsonCatRepository : ICatRepository
    {
        private readonly string _filePath = "cats.json";
        private readonly Dictionary<string, Cat> _cache = new Dictionary<string, Cat>();
        private bool _initialized = false;

        private void EnsureLoaded()
        {
            if (_initialized) return;

            if (!File.Exists(_filePath))
            {
                _initialized = true;
                return;
            }

            var json = File.ReadAllText(_filePath);
            var catDtos = System.Text.Json.JsonSerializer.Deserialize<List<CatPersistenceDto>>(json);

            if (catDtos is { Count: > 0 })
            {
                foreach (var dto in catDtos)
                {
                    if (!string.IsNullOrWhiteSpace(dto.id))
                        _cache[dto.id] = dto.ToEntity();
                }
            }

            _initialized = true;
        }

        public void Add(Cat cat)
        {
            EnsureLoaded();

            if (_cache.ContainsKey(cat.ID))
                throw new ArgumentException($"A cat with ID {cat.ID} already exists.", nameof(cat));

            _cache[cat.ID] = cat;

            SaveToFile();
        }

        public IEnumerable<Cat> GetAll()
        {
            EnsureLoaded();
            return _cache.Values;
        }

        public Cat? GetById(string id)
        {
            EnsureLoaded();
            if (_cache.TryGetValue(id, out var cat))
                return cat;
            return null;
        }

        public Cat? GetByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));

            EnsureLoaded();

            return _cache.Values.FirstOrDefault(cat => cat.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public void Remove(Cat cat)
        {
            EnsureLoaded();
            if (_cache.Remove(cat.ID))
            {
                SaveToFile();
            }
        }

        public void Update(Cat cat)
        {
            EnsureLoaded();

            if (!_cache.ContainsKey(cat.ID))
                throw new ArgumentException($"No cat with ID {cat.ID} exists.", nameof(cat));
            _cache[cat.ID] = cat;
            SaveToFile();
        }

        private void SaveToFile()
        {
            var catDtos = _cache.Values.Select(cat => CatPersistenceMapper.ToDto(cat)).ToList();
            var json = System.Text.Json.JsonSerializer.Serialize(catDtos, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public void Remove(string id)
        {
            EnsureLoaded();
            if (_cache.Remove(id))
            {
                SaveToFile();
            }
        }
    }
}