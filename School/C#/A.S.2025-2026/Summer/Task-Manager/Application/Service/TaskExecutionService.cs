using System;
using System.Threading.Tasks;
using Application.Interface;
using Domain.Entity;

namespace Application.Service
{
    public class TaskExecutionService : ITaskExecutionService
    {
        public async Task ExecuteAsync(Tasks task, Action<Tasks> onStateChanged)
        {
            task.Start();
            onStateChanged(task);
            await Task.Delay(TimeSpan.FromSeconds(task.EstimatedTimeSeconds));
            task.Complete();
            onStateChanged(task);
        }
    }
}