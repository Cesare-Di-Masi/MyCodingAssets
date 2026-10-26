using Domain.Enum;

namespace Application.DTO
{
    public class ReadTaskGroupDTO
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public TaskGroupState State { get; set; }
        public List<ReadTaskDTO> Tasks { get; set; } = new();
    }
}