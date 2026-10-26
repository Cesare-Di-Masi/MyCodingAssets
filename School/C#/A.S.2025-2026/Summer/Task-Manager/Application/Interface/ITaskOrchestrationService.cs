using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entity;

namespace Application.Interface
{
    public interface ITaskOrchestrationService
    {
        Task RunAllAsync(IEnumerable<Tasks> tasks, Action<Tasks> onStateChanged);
    }
}