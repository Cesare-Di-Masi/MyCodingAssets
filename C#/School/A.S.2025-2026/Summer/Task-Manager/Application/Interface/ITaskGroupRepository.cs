using Domain.Entity;

namespace Application.Interface
{
    public interface ITaskGroupRepository
    {
        Task<TaskGroup?> GetTaskGroupByIdAsync(Guid groupId);

        Task<IEnumerable<TaskGroup>> GetAllTaskGroupsAsync();

        Task AddTaskGroupAsync(TaskGroup taskGroup);

        Task UpdateTaskGroupAsync(TaskGroup taskGroup);

        Task DeleteTaskGroupAsync(Guid groupId);
    }
}