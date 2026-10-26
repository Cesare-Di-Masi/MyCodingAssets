using System;
using System.Threading.Tasks;
using Domain.Entity;

namespace Application.Interface
{
    public interface ITaskExecutionService
    {
        Task ExecuteAsync(Tasks task, Action<Tasks> onStateChanged);
    }
}