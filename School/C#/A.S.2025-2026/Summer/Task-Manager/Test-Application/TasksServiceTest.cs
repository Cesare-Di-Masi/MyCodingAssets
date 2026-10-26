using Application.DTO;
using Application.Interface;
using Application.Service;
using Domain.Entity;
using Domain.Enum;
using Moq;

namespace Application.Tests
{
    [TestClass]
    public class TasksServiceTests
    {
        private Mock<ITaskRepository> _taskRepository = null!;
        private Mock<ITaskGroupRepository> _taskGroupRepository = null!;
        private Mock<ITaskOrchestrationService> _orchestrationService = null!;
        private TasksService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _taskRepository = new Mock<ITaskRepository>();
            _taskGroupRepository = new Mock<ITaskGroupRepository>();
            _orchestrationService = new Mock<ITaskOrchestrationService>();
            _service = new TasksService(_taskRepository.Object, _taskGroupRepository.Object, _orchestrationService.Object);
        }

        [TestMethod]
        public async Task CreateTaskAsync_NoGroupId_AddsToTaskRepository()
        {
            var dto = new CreateTaskDTO { Name = "Task1", EstimatedTimeSeconds = 5 };

            var result = await _service.CreateTaskAsync(dto);

            Assert.AreEqual("Task1", result.Name);
            Assert.IsNull(result.GroupId);
            _taskRepository.Verify(r => r.AddTaskAsync(It.IsAny<Tasks>()), Times.Once);
            _taskGroupRepository.Verify(r => r.UpdateTaskGroupAsync(It.IsAny<TaskGroup>()), Times.Never);
        }

        [TestMethod]
        public async Task CreateTaskAsync_WithGroupId_AddsToGroup()
        {
            var group = new TaskGroup("Group1");
            _taskGroupRepository
                .Setup(r => r.GetTaskGroupByIdAsync(group.GroupId))
                .ReturnsAsync(group);

            var dto = new CreateTaskDTO { Name = "Task1", EstimatedTimeSeconds = 5, GroupId = group.GroupId };

            var result = await _service.CreateTaskAsync(dto);

            Assert.AreEqual(group.GroupId, result.GroupId);
            Assert.AreEqual(1, group.Tasks.Count);
            _taskGroupRepository.Verify(r => r.UpdateTaskGroupAsync(group), Times.Once);
            _taskRepository.Verify(r => r.AddTaskAsync(It.IsAny<Tasks>()), Times.Never);
        }

        [TestMethod]
        public async Task CreateTaskAsync_GroupIdNotFound_Throws()
        {
            _taskGroupRepository
                .Setup(r => r.GetTaskGroupByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((TaskGroup?)null);

            var dto = new CreateTaskDTO { Name = "Task1", EstimatedTimeSeconds = 5, GroupId = Guid.NewGuid() };

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.CreateTaskAsync(dto));
        }

        [TestMethod]
        public async Task CreateTaskGroupAsync_AddsGroupToRepository()
        {
            var dto = new CreateTaskGroupDTO { GroupName = "Group1" };

            var result = await _service.CreateTaskGroupAsync(dto);

            Assert.AreEqual("Group1", result.GroupName);
            _taskGroupRepository.Verify(r => r.AddTaskGroupAsync(It.IsAny<TaskGroup>()), Times.Once);
        }

        [TestMethod]
        public async Task GetAllTasksAsync_ReturnsMappedDTOs()
        {
            var task = new Tasks("Task1", 5);
            _taskRepository
                .Setup(r => r.GetAllTasksAsync())
                .ReturnsAsync(new List<Tasks> { task });

            var result = (await _service.GetAllTasksAsync()).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(task.Id, result[0].Id);
            Assert.AreEqual(task.Name, result[0].Name);
        }

        [TestMethod]
        public async Task GetTaskGroupByIdAsync_NotFound_ReturnsNull()
        {
            _taskGroupRepository
                .Setup(r => r.GetTaskGroupByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((TaskGroup?)null);

            var result = await _service.GetTaskGroupByIdAsync(Guid.NewGuid());

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetTaskGroupByIdAsync_Found_ReturnsMappedDTOWithTasks()
        {
            var group = new TaskGroup("Group1");
            var task = new Tasks("Task1", 5);
            group.AddTask(task);

            _taskGroupRepository
                .Setup(r => r.GetTaskGroupByIdAsync(group.GroupId))
                .ReturnsAsync(group);

            var result = await _service.GetTaskGroupByIdAsync(group.GroupId);

            Assert.IsNotNull(result);
            Assert.AreEqual(group.GroupId, result!.GroupId);
            Assert.AreEqual(1, result.Tasks.Count);
            Assert.AreEqual(task.Id, result.Tasks[0].Id);
        }

        [TestMethod]
        public async Task RemoveTaskFromGroupAsync_GroupNotFound_Throws()
        {
            _taskGroupRepository
                .Setup(r => r.GetTaskGroupByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((TaskGroup?)null);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.RemoveTaskFromGroupAsync(Guid.NewGuid(), Guid.NewGuid()));
        }

        [TestMethod]
        public async Task StartAllTasksAsync_RunsOnlyWaitingTasksAndPersistsThem()
        {
            var waitingTask = new Tasks("Task1", 1);
            var completedTask = new Tasks("Task2", 1);
            completedTask.Start();
            completedTask.Complete();

            _taskRepository
                .Setup(r => r.GetAllTasksAsync())
                .ReturnsAsync(new List<Tasks> { waitingTask, completedTask });

            _orchestrationService
                .Setup(o => o.RunAllAsync(It.IsAny<IEnumerable<Tasks>>(), It.IsAny<Action<Tasks>>()))
                .Returns(Task.CompletedTask);

            await _service.StartAllTasksAsync(_ => { });

            _orchestrationService.Verify(
                o => o.RunAllAsync(It.Is<IEnumerable<Tasks>>(t => t.Single() == waitingTask), It.IsAny<Action<Tasks>>()),
                Times.Once);
            _taskRepository.Verify(r => r.UpdateTaskAsync(waitingTask), Times.Once);
            _taskRepository.Verify(r => r.UpdateTaskAsync(completedTask), Times.Never);
        }

        [TestMethod]
        public async Task StartAllTasksAsync_InvokesCallbackWithMappedDTO()
        {
            var task = new Tasks("Task1", 1);

            _taskRepository
                .Setup(r => r.GetAllTasksAsync())
                .ReturnsAsync(new List<Tasks> { task });

            _orchestrationService
                .Setup(o => o.RunAllAsync(It.IsAny<IEnumerable<Tasks>>(), It.IsAny<Action<Tasks>>()))
                .Callback<IEnumerable<Tasks>, Action<Tasks>>((tasks, callback) =>
                {
                    foreach (var t in tasks)
                        callback(t);
                })
                .Returns(Task.CompletedTask);

            ReadTaskDTO? received = null;
            await _service.StartAllTasksAsync(dto => received = dto);

            Assert.IsNotNull(received);
            Assert.AreEqual(task.Id, received!.Id);
        }

        [TestMethod]
        public async Task StartTaskGroupAsync_GroupNotFound_Throws()
        {
            _taskGroupRepository
                .Setup(r => r.GetTaskGroupByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((TaskGroup?)null);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _service.StartTaskGroupAsync(Guid.NewGuid(), _ => { }));
        }

        [TestMethod]
        public async Task StartTaskGroupAsync_RefreshesAndPersistsGroup()
        {
            var group = new TaskGroup("Group1");
            var task = new Tasks("Task1", 1);
            group.AddTask(task);

            _taskGroupRepository
                .Setup(r => r.GetTaskGroupByIdAsync(group.GroupId))
                .ReturnsAsync(group);

            _orchestrationService
                .Setup(o => o.RunAllAsync(It.IsAny<IEnumerable<Tasks>>(), It.IsAny<Action<Tasks>>()))
                .Callback<IEnumerable<Tasks>, Action<Tasks>>((tasks, callback) =>
                {
                    foreach (var t in tasks)
                    {
                        t.Start();
                        callback(t);
                        t.Complete();
                        callback(t);
                    }
                })
                .Returns(Task.CompletedTask);

            await _service.StartTaskGroupAsync(group.GroupId, _ => { });

            Assert.AreEqual(TaskGroupState.Completed, group.State);
            _taskGroupRepository.Verify(r => r.UpdateTaskGroupAsync(group), Times.Once);
        }
    }
}