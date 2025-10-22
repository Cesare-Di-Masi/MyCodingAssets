using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Model.Entities;

namespace Application.Interfaces
{
    public interface ICatRepository
    {
        void Add(Cat cat);
        void Update(Cat cat);
        void Remove(Cat cat);
        Cat? GetById(string id);
        Cat? GetByName(string name);
        IEnumerable<Cat> GetAll();
    }
}
