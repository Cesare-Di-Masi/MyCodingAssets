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
        private readonly IAdoptionInterface _adoptionRepository;

        public CatteryService(
            ICatRepository catRepository,
            IAdopterRepository adopterRepository,
            IAdoptionInterface adoptionRepository)
        {
            _catRepository = catRepository;
            _adopterRepository = adopterRepository;
            _adoptionRepository = adoptionRepository;
        }

        public void RegisterNewCat(CatDto cat)
        {
            if (string.IsNullOrWhiteSpace(cat.Name))
                throw new ArgumentException("Cat name cannot be empty.", nameof(cat));

            var existingCat = _catRepository.GetById(cat.Id);

            if (existingCat != null)
                throw new ArgumentException($"A cat with the name '{cat.Name}' already exists.");
            Cat newCat = cat.ToEntity();

            _catRepository.Add(newCat);
        }

        public void RegisterNewAdopter(AdopterDto adopter)
        {
            if (string.IsNullOrWhiteSpace(adopter.TaxIDCode))
                throw new ArgumentException("Adopter Tax ID Code cannot be empty.", nameof(adopter));
            var existingAdopter = _adopterRepository.GetByTaxIDCode(adopter.TaxIDCode);
            if (existingAdopter != null)
                throw new InvalidOperationException($"An adopter with the Tax ID '{adopter.TaxIDCode}' already exists.");
            var newAdopter = adopter.ToEntity();
            _adopterRepository.Add(newAdopter);
        }

        public void RegisterNewAdoption(AdoptionDto adoption)
        {
            var cat = _catRepository.GetById(adoption.Cat.Id);
            if (cat == null)
                throw new InvalidOperationException($"No cat found with ID '{adoption.Cat.Id}'.");
            var adopter = _adopterRepository.GetByTaxIDCode(adoption.Adopter.TaxIDCode);
            if (adopter == null)
                throw new InvalidOperationException($"No adopter found with Tax ID '{adoption.Adopter.TaxIDCode}'.");
            var newAdoption = adoption.ToEntity();
            _adoptionRepository.Add(newAdoption);
        }

        public string ViewCatInfo(string id)
        {
            var cat = _catRepository.GetById(id);
            if (cat == null)
                throw new InvalidOperationException($"No cat found with ID '{id}'.");
            return cat.ToString();
        }

        public List<Cat> ViewAllCats()
        {
            return _catRepository.GetAll().ToList();
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



    }
}