using Domain.Entity;
using Domain.Enum;

namespace Domain.Tests
{
    [TestClass]
    public class TaskGroupTests
    {
        [TestMethod]
        public void AddTask_SetsGroupIdAndAddsToDictionary()
        {
            var group = new TaskGroup("Group1");
            var task = new Tasks("Task1", 5);

            group.AddTask(task);

            Assert.AreEqual(group.GroupId, task.GroupId);
            Assert.IsTrue(group.Tasks.ContainsKey(task.Id));
        }

        [TestMethod]
        public void AddTask_TaskAlreadyInAnotherGroup_Throws()
        {
            var group1 = new TaskGroup("Group1");
            var group2 = new TaskGroup("Group2");
            var task = new Tasks("Task1", 5);
            group1.AddTask(task);

            Assert.ThrowsException<InvalidOperationException>(() => group2.AddTask(task));
        }

        [TestMethod]
        public void AddTask_TaskNotWaiting_Throws()
        {
            var group = new TaskGroup("Group1");
            var task = new Tasks("Task1", 5);
            task.Start();

            Assert.ThrowsException<InvalidOperationException>(() => group.AddTask(task));
        }

        [TestMethod]
        public void RemoveTask_ClearsGroupId()
        {
            var group = new TaskGroup("Group1");
            var task = new Tasks("Task1", 5);
            group.AddTask(task);

            group.RemoveTask(task.Id);

            Assert.IsNull(task.GroupId);
            Assert.IsFalse(group.Tasks.ContainsKey(task.Id));
        }

        [TestMethod]
        public void RemoveTask_WhileRunning_Throws()
        {
            var group = new TaskGroup("Group1");
            var task = new Tasks("Task1", 5);
            group.AddTask(task);
            task.Start();

            Assert.ThrowsException<InvalidOperationException>(() => group.RemoveTask(task.Id));
        }

        [TestMethod]
        public void CancelGroup_CancelsAllNonTerminalTasks()
        {
            var group = new TaskGroup("Group1");
            var task1 = new Tasks("Task1", 5);
            var task2 = new Tasks("Task2", 5);
            group.AddTask(task1);
            group.AddTask(task2);
            task1.Start();

            group.CancelGroup();

            Assert.AreEqual(TaskState.Canceled, task1.State);
            Assert.AreEqual(TaskState.Canceled, task2.State);
            Assert.AreEqual(TaskGroupState.Canceled, group.State);
        }

        [TestMethod]
        public void CompletedCount_CountsOnlyCompletedTasks()
        {
            var group = new TaskGroup("Group1");
            var task1 = new Tasks("Task1", 5);
            var task2 = new Tasks("Task2", 5);
            group.AddTask(task1);
            group.AddTask(task2);
            task1.Start();
            task1.Complete();

            Assert.AreEqual(1, group.CompletedCount());
        }

        [TestMethod]
        public void RefreshState_AllCompleted_SetsCompleted()
        {
            var group = new TaskGroup("Group1");
            var task = new Tasks("Task1", 5);
            group.AddTask(task);
            task.Start();
            task.Complete();

            group.RefreshState();

            Assert.AreEqual(TaskGroupState.Completed, group.State);
        }

        [TestMethod]
        public void RefreshState_AnyRunning_SetsInProgress()
        {
            var group = new TaskGroup("Group1");
            var task = new Tasks("Task1", 5);
            group.AddTask(task);
            task.Start();

            group.RefreshState();

            Assert.AreEqual(TaskGroupState.InProgress, group.State);
        }

        [TestMethod]
        public void RefreshState_AllCanceled_SetsCanceled()
        {
            var group = new TaskGroup("Group1");
            var task = new Tasks("Task1", 5);
            group.AddTask(task);
            task.Cancel();

            group.RefreshState();

            Assert.AreEqual(TaskGroupState.Canceled, group.State);
        }

        [TestMethod]
        public void RefreshState_NoTasks_SetsWaiting()
        {
            var group = new TaskGroup("Group1");

            group.RefreshState();

            Assert.AreEqual(TaskGroupState.Waiting, group.State);
        }
    }
}