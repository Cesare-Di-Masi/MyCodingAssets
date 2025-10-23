using Domain.Model.Entities;

namespace Application.Interfaces
{
    public interface IAdopterRepository
    {
        void Add(Adopter adopter);

        void Update(Adopter adopter);

        void Remove(Adopter adopter);

        void Remove(string taxIDCode);

        Adopter? GetByTaxIDCode(string code);

        Adopter? GetByName(string firstName, string lastName);

        IEnumerable<Adopter> GetAll();
    }
}