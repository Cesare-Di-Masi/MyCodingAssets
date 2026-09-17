using Domain.Enum;

namespace Infrastructure.Persistence.DTO
{
    public class TaskPersistenceDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double EstimatedTimeSeconds { get; set; }
        public TaskState State { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? GroupId { get; set; }
    }
}