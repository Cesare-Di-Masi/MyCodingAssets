using Application.Service;
using Domain.Entity;
using Domain.Enum;

namespace Application.Tests
{
    [TestClass]
    public class TaskExecutionServiceTests
    {
        [TestMethod]
        public async Task ExecuteAsync_RunsTaskToCompletion()
        {
            var service = new TaskExecutionService();
            var task = new Tasks("Task1", 0.1);

            await service.ExecuteAsync(task, _ => { });

            Assert.AreEqual(TaskState.Completed, task.State);
        }

        [TestMethod]
        public async Task ExecuteAsync_InvokesCallbackForEachTransition()
        {
            var service = new TaskExecutionService();
            var task = new Tasks("Task1", 0.1);
            var statesSeen = new List<TaskState>();

            await service.ExecuteAsync(task, t => statesSeen.Add(t.State));

            CollectionAssert.AreEqual(new[] { TaskState.Running, TaskState.Completed }, statesSeen);
        }
    }
}