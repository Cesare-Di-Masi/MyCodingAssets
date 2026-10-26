using Domain.Entity;
using Infrastructure.Persistence.DTO;

namespace Infrastructure.Persistence.Mapper
{
    public static class TaskPersistenceMapper
    {
        public static TaskPersistenceDTO ToDTO(Tasks task)
        {
            return new TaskPersistenceDTO
            {
                Id = task.Id,
                Name = task.Name,
                EstimatedTimeSeconds = task.EstimatedTimeSeconds,
                State = task.State,
                CreatedAt = task.CreatedAt,
                GroupId = task.GroupId
            };
        }

        public static Tasks ToDomain(TaskPersistenceDTO dto)
        {
            return new Tasks(dto.Id, dto.Name, dto.EstimatedTimeSeconds, dto.CreatedAt, dto.State, dto.GroupId);
        }
    }
}