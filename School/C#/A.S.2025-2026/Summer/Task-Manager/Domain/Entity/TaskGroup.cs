using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Enum;

namespace Domain.Entity
{
    public class TaskGroup
    {
        public Guid GroupId { get; private set; }
        public string GroupName { get; private set; }
        public TaskGroupState State { get; private set; }
        public Dictionary<Guid, Tasks> Tasks { get; private set; } = new();

        public TaskGroup(string groupName)
        {
            GroupId = Guid.NewGuid();
            GroupName = groupName;
            State = TaskGroupState.Waiting;
        }

        public TaskGroup(Guid groupId, string groupName, TaskGroupState state, IEnumerable<Tasks> tasks)
        {
            GroupId = groupId;
            GroupName = groupName;
            State = state;
            Tasks = tasks.ToDictionary(t => t.Id);
        }

        public void AddTask(Tasks task)
        {
            if (task.GroupId.HasValue && task.GroupId != GroupId)
                throw new InvalidOperationException($"Task '{task.Name}' already belongs to another group.");
            if (task.State != TaskState.Waiting)
                throw new InvalidOperationException($"Task '{task.Name}' is already {task.State} and cannot be added.");

            task.GroupId = GroupId;
            Tasks[task.Id] = task;
            RefreshState();
        }

        public void RemoveTask(Guid taskId)
        {
            if (!Tasks.TryGetValue(taskId, out var task))
                throw new ArgumentException($"Task with ID {taskId} not found.");
            if (task.State == TaskState.Running)
                throw new InvalidOperationException($"Cannot remove running task '{task.Name}'.");

            task.GroupId = null;
            Tasks.Remove(taskId);
            RefreshState();
        }

        public void RefreshState()
        {
            if (Tasks.Count == 0)
            {
                State = TaskGroupState.Waiting;
                return;
            }

            bool allCompleted = Tasks.Values.All(t => t.State == TaskState.Completed);
            bool anyRunning = Tasks.Values.Any(t => t.State == TaskState.Running);
            bool allCanceled = Tasks.Values.All(t => t.State == TaskState.Canceled);

            if (allCompleted)
                State = TaskGroupState.Completed;
            else if (allCanceled)
                State = TaskGroupState.Canceled;
            else if (anyRunning)
                State = TaskGroupState.InProgress;
            else
                State = TaskGroupState.Waiting;
        }
    }
}