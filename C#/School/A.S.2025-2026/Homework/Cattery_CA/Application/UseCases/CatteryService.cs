using Application.Dto;
using Application.Interfaces;
using Application.Mappers;
using Domain.Model.Entities;

namespace Application.UseCases
{
    public class CatteryService
    {
        private readonly ICatRepository _catRepository;
        private readonly IAdopterRepository _adopterRepository;
        private readonly IAdoptionRepository _adoptionRepository;

        public CatteryService(
            ICatRepository catRepository,
            IAdopterRepository adopterRepository,
            IAdoptionRepository adoptionRepository)
        {
            _catRepository = catRepository;
            _adopterRepository = adopterRepository;
            _adoptionRepository = adoptionRepository;
        }

        public void RegisterNewCat(CatDto cat)
        {
            if (string.IsNullOrWhiteSpace(cat.Name))
                throw new ArgumentException("Cat name cannot be empty.", nameof(cat));

            Cat newCat = cat.ToEntity();

            var existingCat = _catRepository.GetById(newCat.ID);

            if (existingCat != null)
                throw new ArgumentException($"A cat with the name '{cat.Name}' already exists.");

            _catRepository.Add(newCat);
        }

        public void UpdateCat(CatDto cat)
        {
            var existingCat = _catRepository.GetById(cat.Id);
            if (existingCat == null)
                throw new InvalidOperationException($"No cat found with ID '{cat.Id}'.");

            Cat catEntity = cat.ToEntity();

            _catRepository.Update(catEntity);
        }

        public void RegisterNewAdopter(AdopterDto adopter)
        {
            if (string.IsNullOrWhiteSpace(adopter.TaxIDCode))
                throw new ArgumentException("Adopter Tax ID Code cannot be empty.", nameof(adopter));
            var newAdopter = adopter.ToEntity();
            var existingAdopter = _adopterRepository.GetByTaxIDCode(adopter.TaxIDCode);
            if (existingAdopter != null)
                throw new InvalidOperationException($"An adopter with the Tax ID '{adopter.TaxIDCode}' already exists.");

            _adopterRepository.Add(newAdopter);
        }

        public void RegisterNewAdoption(AdoptionDto adoption)
        {
            var newAdoption = adoption.ToEntity();
            var cat = _catRepository.GetById(adoption.Cat.Id);
            if (cat == null)
                throw new InvalidOperationException($"No cat found with ID '{adoption.Cat.Id}'.");
            var adopter = _adopterRepository.GetByTaxIDCode(adoption.Adopter.TaxIDCode);
            if (adopter == null)
                throw new InvalidOperationException($"No adopter found with Tax ID '{adoption.Adopter.TaxIDCode}'.");

            _adoptionRepository.Add(newAdoption);
        }

        public string ViewCatInfo(string id)
        {
            var cat = _catRepository.GetById(id);
            if (cat == null)
                throw new InvalidOperationException($"No cat found with ID '{id}'.");
            return cat.ToString();
        }

        public List<CatDto> ViewAllCats()
        {
            List<Cat> cats = _catRepository.GetAll().ToList();
            List<CatDto> dtos = new List<CatDto>();
            for (int i = 0; i < cats.Count(); i++)
            {
                dtos.Add(cats[i].ToDto());
            }
            return dtos;
        }

        public void RemoveCat(string id)
        {
            var cat = _catRepository.GetById(id);
            if (cat == null)
                throw new InvalidOperationException($"No cat found with ID '{id}'.");
            _catRepository.Remove(cat);
        }

        public void RemoveCat(CatDto cat)
        {
            var existingCat = _catRepository.GetById(cat.Id);
            if (existingCat == null)
                throw new InvalidOperationException($"No cat found with ID '{cat.Id}'.");
            _catRepository.Remove(existingCat);
        }

        public void RemoveAdopter(string taxId)
        {
            var adopter = _adopterRepository.GetByTaxIDCode(taxId);
            if (adopter == null)
                throw new InvalidOperationException($"No adopter found with Tax ID '{taxId}'.");
            _adopterRepository.Remove(adopter);
        }

        public void RemoveAdopter(AdopterDto adopter)
        {
            var existingAdopter = _adopterRepository.GetByTaxIDCode(adopter.TaxIDCode);
            if (existingAdopter == null)
                throw new InvalidOperationException($"No adopter found with Tax ID '{adopter.TaxIDCode}'.");
            _adopterRepository.Remove(existingAdopter);
        }

        public void RemoveAdoption(AdoptionDto adoption)
        {
            var existingAdoption = _adoptionRepository.GetByDate(adoption.AdoptionDate)
                .FirstOrDefault(a => a.Cat.ID == adoption.Cat.Id && a.Adopter.TaxIDCode.Value == adoption.Adopter.TaxIDCode);
            if (existingAdoption == null)
                throw new InvalidOperationException("No matching adoption found.");
            _adoptionRepository.Remove(existingAdoption);
        }

        public List<Adopter> ViewAllAdopters()
        {
            return _adopterRepository.GetAll().ToList();
        }

        public List<Adoption> ViewAllAdoptions()
        {
            return _adoptionRepository.GetAll().ToList();
        }

        public string ViewAdopterInfo(string taxId)
        {
            var adopter = _adopterRepository.GetByTaxIDCode(taxId);
            if (adopter == null)
                throw new InvalidOperationException($"No adopter found with Tax ID '{taxId}'.");
            return adopter.ToString();
        }

        public int GetFemaleCatsCount()
        {
            return _catRepository.GetAll().Count(cat => cat.IsMale == false);
        }

        public int GetMaleCatsCount()
        {
            return _catRepository.GetAll().Count(cat => cat.IsMale == true);
        }

        public int GetTotalCatsCount()
        {
            return _catRepository.GetAll().Count();
        }

        public void DeleteCat(string id)
        {
            var cat = _catRepository.GetById(id);
            if (cat == null)
                throw new InvalidOperationException($"No cat found with ID '{id}'.");
            _catRepository.Remove(id);
        }
    }
}