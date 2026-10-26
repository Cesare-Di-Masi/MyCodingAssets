using Application.Interface;
using Application.Service;
using Domain.Entity;
using Domain.Enum;
using Moq;

namespace Application.Tests
{
    [TestClass]
    public class TaskOrchestrationServiceTests
    {
        [TestMethod]
        public async Task RunAllAsync_CallsExecuteAsyncForEveryTask()
        {
            var executionServiceMock = new Mock<ITaskExecutionService>();
            executionServiceMock
                .Setup(s => s.ExecuteAsync(It.IsAny<Tasks>(), It.IsAny<Action<Tasks>>()))
                .Returns(Task.CompletedTask);

            var tasks = new List<Tasks>
            {
                new Tasks("Task1", 1),
                new Tasks("Task2", 1)
            };

            var orchestrationService = new TaskOrchestrationService(executionServiceMock.Object);

            await orchestrationService.RunAllAsync(tasks, _ => { });

            foreach (var task in tasks)
                executionServiceMock.Verify(s => s.ExecuteAsync(task, It.IsAny<Action<Tasks>>()), Times.Once);
        }

        [TestMethod]
        public async Task RunAllAsync_WithRealExecutionService_CompletesAllTasksAndInvokesCallback()
        {
            var executionService = new TaskExecutionService();
            var orchestrationService = new TaskOrchestrationService(executionService);
            var tasks = new List<Tasks>
            {
                new Tasks("Task1", 0.1),
                new Tasks("Task2", 0.1)
            };
            var callbackCount = 0;

            await orchestrationService.RunAllAsync(tasks, _ => Interlocked.Increment(ref callbackCount));

            foreach (var task in tasks)
                Assert.AreEqual(TaskState.Completed, task.State);

            Assert.AreEqual(4, callbackCount);
        }
    }
}