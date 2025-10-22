using Application.Dto;
using Domain.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /*
     * questa interfaccia va nel layer Application, perché rappresenta 
     * un contratto che il dominio e l’Application usano senza sapere 
     * come sarà implementata la persistenza (JSON, DB, ecc.).
     */
    public interface IVeterinaryRepository
    {
        Veterinary? GetByName(string firstName, string lastName);
        IEnumerable<Veterinary> GetAll();
        void Add(Veterinary vet);
        void Update(Veterinary vet);
        void Delete(string firstName, string lastName);
    }

}
