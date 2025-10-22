using Application.Dto;
using Domain.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IVeterinaryVisitRepository
    {
        IEnumerable<VeterinaryVisit> GetByAnimal(string animalName);
        IEnumerable<VeterinaryVisit> GetByDate(DateTime visitDate);
        void Add(VeterinaryVisit visit);
        void Delete(VeterinaryVisit visit);
        void Delete(DateTime visitDate);
        void Update(VeterinaryVisit visit);
    }
}
