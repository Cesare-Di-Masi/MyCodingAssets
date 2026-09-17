using Application.DTO;

namespace Application.Interface
{
    public interface ITasksService
    {
        Task<IEnumerable<ReadTaskGroupDTO>> GetAllTaskGroupsAsync();

        Task<IEnumerable<ReadTaskDTO>> GetAllTasksAsync();

        Task<ReadTaskGroupDTO?> GetTaskGroupByIdAsync(Guid groupId);

        Task<ReadTaskDTO?> GetTaskByIdAsync(Guid taskId);

        Task<ReadTaskDTO> CreateTaskAsync(CreateTaskDTO dto);

        Task DeleteTaskAsync(Guid taskId);

        Task<ReadTaskGroupDTO> CreateTaskGroupAsync(CreateTaskGroupDTO dto);

        Task DeleteTaskGroupAsync(Guid groupId);

        Task RemoveTaskFromGroupAsync(Guid groupId, Guid taskId);

        Task StartAllTasksAsync(Action<ReadTaskDTO> onTaskStateChanged);

        Task StartTaskGroupAsync(Guid groupId, Action<ReadTaskDTO> onTaskStateChanged);
    }
}