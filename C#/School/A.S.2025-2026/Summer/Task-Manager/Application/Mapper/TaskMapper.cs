using Application.DTO;
using Domain.Entity;

namespace Application.Mapper
{
    public static class TaskMapper
    {
        public static ReadTaskDTO ToDTO(Tasks task)
        {
            return new ReadTaskDTO
            {
                Id = task.Id,
                Name = task.Name,
                EstimatedTimeSeconds = task.EstimatedTimeSeconds,
                State = task.State,
                CreatedAt = task.CreatedAt,
                GroupId = task.GroupId
            };
        }
    }
}