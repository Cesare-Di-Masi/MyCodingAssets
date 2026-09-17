using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interface;
using Domain.Entity;

namespace Application.Service
{
    public class TaskOrchestrationService : ITaskOrchestrationService
    {
        private readonly ITaskExecutionService _executionService;

        public TaskOrchestrationService(ITaskExecutionService executionService)
        {
            _executionService = executionService;
        }

        public async Task RunAllAsync(IEnumerable<Tasks> tasks, Action<Tasks> onStateChanged)
        {
            var runs = tasks.Select(t => _executionService.ExecuteAsync(t, onStateChanged));
            await Task.WhenAll(runs);
        }
    }
}