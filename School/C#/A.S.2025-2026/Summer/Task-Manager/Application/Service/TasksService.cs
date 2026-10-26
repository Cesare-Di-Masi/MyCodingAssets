using Application.DTO;
using Application.Interface;
using Application.Mapper;
using Domain.Entity;
using Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Service
{
    public class TasksService : ITasksService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ITaskGroupRepository _taskGroupRepository;
        private readonly ITaskOrchestrationService _orchestrationService;

        public TasksService(
            ITaskRepository taskRepository,
            ITaskGroupRepository taskGroupRepository,
            ITaskOrchestrationService orchestrationService)
        {
            _taskRepository = taskRepository;
            _taskGroupRepository = taskGroupRepository;
            _orchestrationService = orchestrationService;
        }

        // --- Queries (unchanged) ---
        public async Task<IEnumerable<ReadTaskGroupDTO>> GetAllTaskGroupsAsync()
        {
            var groups = await _taskGroupRepository.GetAllTaskGroupsAsync();
            return groups.Select(TaskGroupMapper.ToDTO);
        }

        public async Task<IEnumerable<ReadTaskDTO>> GetAllTasksAsync()
        {
            var tasks = await _taskRepository.GetAllTasksAsync();
            return tasks.Select(TaskMapper.ToDTO);
        }

        public async Task<ReadTaskDTO?> GetTaskByIdAsync(Guid taskId)
        {
            var task = await _taskRepository.GetTaskByIdAsync(taskId);
            return task == null ? null : TaskMapper.ToDTO(task);
        }

        public async Task<ReadTaskGroupDTO?> GetTaskGroupByIdAsync(Guid groupId)
        {
            var group = await _taskGroupRepository.GetTaskGroupByIdAsync(groupId);
            return group == null ? null : TaskGroupMapper.ToDTO(group);
        }

        // --- Commands (unchanged) ---
        public async Task<ReadTaskDTO> CreateTaskAsync(CreateTaskDTO dto)
        {
            var task = new Tasks(dto.Name, dto.EstimatedTimeSeconds);
            if (dto.GroupId.HasValue)
            {
                var group = await _taskGroupRepository.GetTaskGroupByIdAsync(dto.GroupId.Value);
                if (group == null)
                    throw new InvalidOperationException($"Task group {dto.GroupId} not found.");
                group.AddTask(task);
                await _taskGroupRepository.UpdateTaskGroupAsync(group);
            }
            else
            {
                await _taskRepository.AddTaskAsync(task);
            }
            return TaskMapper.ToDTO(task);
        }

        public async Task DeleteTaskAsync(Guid taskId) => await _taskRepository.DeleteTaskAsync(taskId);

        public async Task<ReadTaskGroupDTO> CreateTaskGroupAsync(CreateTaskGroupDTO dto)
        {
            var group = new TaskGroup(dto.GroupName);
            await _taskGroupRepository.AddTaskGroupAsync(group);
            return TaskGroupMapper.ToDTO(group);
        }

        public async Task DeleteTaskGroupAsync(Guid groupId) => await _taskGroupRepository.DeleteTaskGroupAsync(groupId);

        public async Task RemoveTaskFromGroupAsync(Guid groupId, Guid taskId)
        {
            var group = await _taskGroupRepository.GetTaskGroupByIdAsync(groupId);
            if (group == null)
                throw new InvalidOperationException($"Task group {groupId} not found.");
            group.RemoveTask(taskId);
            await _taskGroupRepository.UpdateTaskGroupAsync(group);
        }

        // --- Execution (now uses orchestration) ---
        public async Task StartAllTasksAsync(Action<ReadTaskDTO> onTaskStateChanged)
        {
            var tasks = await _taskRepository.GetAllTasksAsync();
            var waiting = tasks.Where(t => t.State == TaskState.Waiting).ToList();
            await _orchestrationService.RunAllAsync(waiting, task =>
            {
                onTaskStateChanged(TaskMapper.ToDTO(task));
                _taskRepository.UpdateTaskAsync(task); // persist each state change
            });
        }

        public async Task StartTaskGroupAsync(Guid groupId, Action<ReadTaskDTO> onTaskStateChanged)
        {
            var group = await _taskGroupRepository.GetTaskGroupByIdAsync(groupId);
            if (group == null)
                throw new InvalidOperationException($"Task group {groupId} not found.");
            var waiting = group.Tasks.Values.Where(t => t.State == TaskState.Waiting).ToList();
            await _orchestrationService.RunAllAsync(waiting, task =>
            {
                onTaskStateChanged(TaskMapper.ToDTO(task));
                _taskRepository.UpdateTaskAsync(task);
            });
            group.RefreshState();
            await _taskGroupRepository.UpdateTaskGroupAsync(group);
        }
    }
}