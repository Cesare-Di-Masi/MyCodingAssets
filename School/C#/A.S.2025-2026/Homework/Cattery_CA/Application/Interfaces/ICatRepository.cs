using Domain.Model.Entities;

namespace Application.Interfaces
{
    public interface ICatRepository
    {
        void Add(Cat cat);

        void Update(Cat cat);

        void Remove(Cat cat);

        void Remove(string id);

        Cat? GetById(string id);

        Cat? GetByName(string name);

        IEnumerable<Cat> GetAll();
    }
}