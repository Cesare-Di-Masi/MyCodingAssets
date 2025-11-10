using Domain.Model.Entities;

namespace Application.Interfaces
{
    public interface IAdoptionRepository
    {
        void Add(Adoption adoption);

        void Remove(Adoption adoption);

        void Remove(DateOnly adoptionDate);

        IEnumerable<Adoption> GetAll();

        IEnumerable<Adoption> GetByAdopter(string adopterTaxID);

        IEnumerable<Adoption> GetByDate(DateOnly adoptionDate);

        IEnumerable<Adoption> GetByCat(string catID);
    }
}