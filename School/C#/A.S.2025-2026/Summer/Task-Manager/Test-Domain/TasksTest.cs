using Domain.Entity;
using Domain.Enum;

namespace Domain.Tests
{
    [TestClass]
    public class TasksTests
    {
        [TestMethod]
        public void Constructor_ValidInput_StartsWaiting()
        {
            var task = new Tasks("Task1", 5);

            Assert.AreEqual(TaskState.Waiting, task.State);
            Assert.AreEqual("Task1", task.Name);
            Assert.AreEqual(5, task.EstimatedTimeSeconds);
            Assert.IsNull(task.GroupId);
        }

        [TestMethod]
        public void Constructor_EmptyName_Throws()
        {
            Assert.ThrowsException<ArgumentException>(() => new Tasks("", 5));
        }

        [TestMethod]
        public void Constructor_NonPositiveEstimatedTime_Throws()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new Tasks("Task1", 0));
        }

        [TestMethod]
        public void Start_WhenWaiting_SetsRunning()
        {
            var task = new Tasks("Task1", 5);

            task.Start();

            Assert.AreEqual(TaskState.Running, task.State);
        }

        [TestMethod]
        public void Start_WhenNotWaiting_Throws()
        {
            var task = new Tasks("Task1", 5);
            task.Start();

            Assert.ThrowsException<InvalidOperationException>(() => task.Start());
        }

        [TestMethod]
        public void Complete_WhenRunning_SetsCompleted()
        {
            var task = new Tasks("Task1", 5);
            task.Start();

            task.Complete();

            Assert.AreEqual(TaskState.Completed, task.State);
        }

        [TestMethod]
        public void Complete_WhenNotRunning_Throws()
        {
            var task = new Tasks("Task1", 5);

            Assert.ThrowsException<InvalidOperationException>(() => task.Complete());
        }

        [TestMethod]
        public void Cancel_WhenWaiting_SetsCanceled()
        {
            var task = new Tasks("Task1", 5);

            task.Cancel();

            Assert.AreEqual(TaskState.Canceled, task.State);
        }

        [TestMethod]
        public void Cancel_WhenRunning_SetsCanceled()
        {
            var task = new Tasks("Task1", 5);
            task.Start();

            task.Cancel();

            Assert.AreEqual(TaskState.Canceled, task.State);
        }

        [TestMethod]
        public void Cancel_WhenCompleted_Throws()
        {
            var task = new Tasks("Task1", 5);
            task.Start();
            task.Complete();

            Assert.ThrowsException<InvalidOperationException>(() => task.Cancel());
        }

        [TestMethod]
        public void PropertyChanged_RaisedOnStateChange()
        {
            var task = new Tasks("Task1", 5);
            var raised = false;
            task.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(Tasks.State))
                    raised = true;
            };

            task.Start();

            Assert.IsTrue(raised);
        }
    }
}