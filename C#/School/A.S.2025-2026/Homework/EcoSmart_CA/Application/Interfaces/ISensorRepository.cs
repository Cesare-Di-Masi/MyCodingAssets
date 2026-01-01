using Domain.Entity;
using Domain.ValueObject;

namespace Application.Interfaces
{
    public interface ISensorRepository
    {
        Task<List<Measurement>> GetAllDataAsync();
    }
}