using Application.Interfaces;
using Application.Mappers;
using Domain.Model.Entities;
using Infrastructure.Dto;
using Infrastructure.Mapper;

namespace Infrastructure.Repositories
{
    public class JsonAdopterRepository : IAdopterRepository
    {
        private readonly string _filePath = "adopters.json";
        private readonly Dictionary<string, Adopter> _cache = new Dictionary<string, Adopter>();
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
            var adopterDtos = System.Text.Json.JsonSerializer.Deserialize<List<AdopterPersistenceDto>>(json);

            foreach (var dto in adopterDtos ?? new List<AdopterPersistenceDto>())
            {
                var adopter = dto.ToEntity();
                _cache[adopter.TaxIDCode.Value] = adopter;
            }

            _initialized = true;
        }

        public void Add(Adopter adopter)
        {
            EnsureLoaded();
            if (_cache.ContainsKey(adopter.TaxIDCode.Value))
                throw new ArgumentException($"An adopter with Tax ID {adopter.TaxIDCode.Value} already exists.", nameof(adopter));

            _cache[adopter.TaxIDCode.Value] = adopter;
        }

        public IEnumerable<Adopter> GetAll()
        {
            EnsureLoaded();
            return _cache.Values;
        }

        public Adopter? GetByTaxIDCode(string code)
        {
            EnsureLoaded();
            return _cache.TryGetValue(code, out var adopter) ? adopter : null;
        }

        public void Remove(Adopter adopter)
        {
            EnsureLoaded();
            if (_cache.Remove(adopter.TaxIDCode.Value))
            {
                SaveToFile();
            }
        }

        public void Remove(string taxIDCode)
        {
            EnsureLoaded();
            if (_cache.Remove(taxIDCode))
            {
                SaveToFile();
            }
        }

        public void Update(Adopter adopter)
        {
            EnsureLoaded();
            if (!_cache.ContainsKey(adopter.TaxIDCode.Value))
                throw new ArgumentException($"No adopter found with Tax ID {adopter.TaxIDCode.Value}.", nameof(adopter));
            _cache[adopter.TaxIDCode.Value] = adopter;
            SaveToFile();
        }

        public Adopter? GetByName(string firstName, string lastName)
        {
            EnsureLoaded();
            return _cache.Values.FirstOrDefault(a => a.FullName.First.Equals(firstName, StringComparison.OrdinalIgnoreCase) &&
                                                     a.FullName.Last.Equals(lastName, StringComparison.OrdinalIgnoreCase));
        }

        private void SaveToFile()
        {
            var adopterDtos = _cache.Values.Select(AdopterMapper.ToDto).ToList();
            var json = System.Text.Json.JsonSerializer.Serialize(adopterDtos, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}