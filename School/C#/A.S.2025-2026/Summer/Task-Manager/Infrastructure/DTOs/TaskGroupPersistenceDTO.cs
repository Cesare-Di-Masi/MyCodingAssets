using Domain.Enum;

namespace Infrastructure.Persistence.DTO
{
    public class TaskGroupPersistenceDTO
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public TaskGroupState State { get; set; }
        public List<TaskPersistenceDTO> Tasks { get; set; } = new();
    }
}