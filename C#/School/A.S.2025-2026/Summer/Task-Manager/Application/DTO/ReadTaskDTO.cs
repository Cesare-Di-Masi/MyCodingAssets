using Domain.Enum;

namespace Application.DTO
{
    public class ReadTaskDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double EstimatedTimeSeconds { get; set; }
        public TaskState State { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? GroupId { get; set; }
    }
}