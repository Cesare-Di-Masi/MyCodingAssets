using Domain.Entity;

namespace Application.Interface
{
    public interface ITaskRepository
    {
        Task<Tasks?> GetTaskByIdAsync(Guid taskId);

        Task<IEnumerable<Tasks>> GetAllTasksAsync();

        Task AddTaskAsync(Tasks task);

        Task UpdateTaskAsync(Tasks task);

        Task DeleteTaskAsync(Guid taskId);
    }
}