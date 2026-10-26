using System;
using Domain.Enum;

namespace Domain.Entity
{
    public class Tasks
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public double EstimatedTimeSeconds { get; private set; }
        public TaskState State { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public Guid? GroupId { get; internal set; }

        public Tasks(string name, double estimatedTimeSeconds)
        {
            Id = Guid.NewGuid();
            Name = name;
            EstimatedTimeSeconds = estimatedTimeSeconds;
            State = TaskState.Waiting;
            CreatedAt = DateTime.UtcNow;
            GroupId = null;
        }

        public Tasks(Guid id, string name, double estimatedTimeSeconds, DateTime createdAt, TaskState state, Guid? groupId = null)
        {
            Id = id;
            Name = name;
            EstimatedTimeSeconds = estimatedTimeSeconds;
            CreatedAt = createdAt;
            State = state;
            GroupId = groupId;
        }

        public void Start()
        {
            if (State != TaskState.Waiting)
                throw new InvalidOperationException($"Cannot start task '{Name}' because it is {State}.");
            State = TaskState.Running;
        }

        public void Complete()
        {
            if (State != TaskState.Running)
                throw new InvalidOperationException($"Cannot complete task '{Name}' because it is {State}.");
            State = TaskState.Completed;
        }

        public void Cancel()
        {
            if (State == TaskState.Completed || State == TaskState.Canceled)
                throw new InvalidOperationException($"Cannot cancel task '{Name}' because it is {State}.");
            State = TaskState.Canceled;
        }
    }
}