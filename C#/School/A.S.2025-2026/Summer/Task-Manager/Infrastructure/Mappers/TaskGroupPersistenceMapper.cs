using Domain.Entity;
using Infrastructure.Persistence.DTO;

namespace Infrastructure.Persistence.Mapper
{
    public static class TaskGroupPersistenceMapper
    {
        public static TaskGroupPersistenceDTO ToDTO(TaskGroup group)
        {
            return new TaskGroupPersistenceDTO
            {
                GroupId = group.GroupId,
                GroupName = group.GroupName,
                State = group.State,
                Tasks = group.Tasks.Values.Select(TaskPersistenceMapper.ToDTO).ToList()
            };
        }

        public static TaskGroup ToDomain(TaskGroupPersistenceDTO dto)
        {
            var tasks = dto.Tasks.Select(TaskPersistenceMapper.ToDomain);
            return new TaskGroup(dto.GroupId, dto.GroupName, dto.State, tasks);
        }
    }
}