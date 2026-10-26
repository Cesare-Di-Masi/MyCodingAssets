using Application.Interfaces;
using Application.Mappers;
using Domain.Model.Entities;
using Infrastructure.Dto;
using Infrastructure.Mapper;

namespace Infrastructure.Repositories
{
    public class JsonAdoptionRepository : IAdoptionRepository
    {
        private readonly string _filePath = "adoptions.json";
        private readonly Dictionary<string, Adoption> _cache = new Dictionary<string, Adoption>();
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
            var adoptionDtos = System.Text.Json.JsonSerializer.Deserialize<List<AdoptionPersistenceDto>>(json);

            foreach (var dto in adoptionDtos ?? new List<AdoptionPersistenceDto>())
            {
                if (dto.adopterTax == null || dto.cat.id == null)
                    continue;
                var adoption = dto.ToEntity();
                string key = $"{adoption.AdoptionDate.ToString("yyyyMMdd")}_{adoption.Cat.ID}_{adoption.Adopter.TaxIDCode.Value}";
                _cache[key] = adoption;
            }
        }

        public void Add(Adoption adoption)
        {
            EnsureLoaded();
            string key = $"{adoption.AdoptionDate.ToString("yyyyMMdd")}_{adoption.Cat.ID}_{adoption.Adopter.TaxIDCode.Value}";
            if (_cache.ContainsKey(key))
                throw new ArgumentException($"An adoption record for cat {adoption.Cat.ID} by adopter {adoption.Adopter.TaxIDCode.Value} on date {adoption.AdoptionDate} already exists.", nameof(adoption));
            _cache[key] = adoption;
            SaveToFile();
        }

        public IEnumerable<Adoption> GetAll()
        {
            EnsureLoaded();
            return _cache.Values;
        }

        public IEnumerable<Adoption> GetByAdopter(string adopterTaxID)
        {
            EnsureLoaded();
            return _cache.Values.Where(a => a.Adopter.TaxIDCode.Value == adopterTaxID);
        }

        public IEnumerable<Adoption> GetByCat(string catID)
        {
            EnsureLoaded();
            return _cache.Values.Where(a => a.Cat.ID == catID);
        }

        public IEnumerable<Adoption> GetByDate(DateOnly adoptionDate)
        {
            EnsureLoaded();
            return _cache.Values.Where(a => a.AdoptionDate == adoptionDate);
        }

        public void Remove(Adoption adoption)
        {
            EnsureLoaded();
            string key = $"{adoption.AdoptionDate.ToString("yyyyMMdd")}_{adoption.Cat.ID}_{adoption.Adopter.TaxIDCode.Value}";
            if (_cache.Remove(key))
                SaveToFile();
        }

        public void Remove(DateOnly adoptionDate)
        {
            EnsureLoaded();
            var keysToRemove = _cache.Keys.Where(k => k.StartsWith(adoptionDate.ToString("yyyyMMdd"))).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
            SaveToFile();
        }

        private void SaveToFile()
        {
            var adoptionDtos = _cache.Values.Select(AdoptionMapper.ToDto).ToList();
            var json = System.Text.Json.JsonSerializer.Serialize(adoptionDtos, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}