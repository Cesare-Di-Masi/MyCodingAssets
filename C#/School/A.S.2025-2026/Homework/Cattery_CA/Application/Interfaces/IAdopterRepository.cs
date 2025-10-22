using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Model.Entities;

namespace Application.Interfaces
{
    public interface IAdopterRepository
    {
        void Add(Adopter adopter);
        void Update(Adopter adopter);
        void Remove(Adopter adopter);
        Adopter? GetByName(string firtsName, string lastName);
        IEnumerable<Adopter> GetAll();
    }
}
