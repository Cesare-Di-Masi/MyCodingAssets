using Application.DTO;
using Domain.Entity;

namespace Application.Mapper
{
    public static class TaskGroupMapper
    {
        public static ReadTaskGroupDTO ToDTO(TaskGroup group)
        {
            return new ReadTaskGroupDTO
            {
                GroupId = group.GroupId,
                GroupName = group.GroupName,
                State = group.State,
                Tasks = group.Tasks.Values.Select(TaskMapper.ToDTO).ToList()
            };
        }
    }
}