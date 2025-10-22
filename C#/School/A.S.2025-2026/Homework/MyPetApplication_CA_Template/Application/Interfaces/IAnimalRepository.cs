using Domain.Model.Entities;

namespace Application.Interfaces
{
    public interface IAnimalRepository
    {
        void Add(Animal animal);
        void Update(Animal animal);
        void Remove(string name);
        Animal? GetByName(string name);
        IEnumerable<Animal> GetAll();
    }
}
