using Application.Dto;
using Application.Interfaces;
using Application.Mappers;
using Domain.Model.Entities;
using Infrastructure.Dto;
using Infrastructure.Mapper;

namespace Infrastructure.Repositories
{
    public class JsonAdoptionRepository : IAdoptionInterface
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
                var adoption = dto.ToEntity();
                string key = $"{adoption.AdoptionDate.ToString("yyyyMMdd")}_{adoption.Cat.ID}_{adoption.Adopter.TaxIDCode.Value}";
                _cache[key] = adoption;
            }
        }

        public void Add(Adoption adoption)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Adoption> GetAll()
        {
            EnsureLoaded();
            return _cache.Values;
        }

        public IEnumerable<Adoption> GetByAdopter(string adopterTaxID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Adoption> GetByCat(string catID)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Adoption> GetByDate(DateOnly adoptionDate)
        {
            throw new NotImplementedException();
        }

        public void Remove(Adoption adoption)
        {
            throw new NotImplementedException();
        }

        public void Remove(DateOnly adoptionDate)
        {
            throw new NotImplementedException();
        }

        private void SaveToFile()
        {
            var adoptionDtos = _cache.Values.Select(AdoptionMapper.ToDto).ToList();
            var json = System.Text.Json.JsonSerializer.Serialize(adoptionDtos, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}