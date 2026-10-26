using Application.Interface;
using Domain.Entity;
using Infrastructure.Persistence.DTO;
using Infrastructure.Persistence.Mapper;
using System.Text.Json;

namespace Infrastructure.Repository
{
    public class JsonTaskGroupRepository : ITaskGroupRepository
    {
        private readonly string _filePath;
        private readonly object _lock = new();

        public JsonTaskGroupRepository(string filePath)
        {
            _filePath = filePath;
        }

        private List<TaskGroupPersistenceDTO> ReadAll()
        {
            if (!File.Exists(_filePath))
                return new List<TaskGroupPersistenceDTO>();

            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
                return new List<TaskGroupPersistenceDTO>();

            return JsonSerializer.Deserialize<List<TaskGroupPersistenceDTO>>(json) ?? new List<TaskGroupPersistenceDTO>();
        }

        private void WriteAll(List<TaskGroupPersistenceDTO> groups)
        {
            var json = JsonSerializer.Serialize(groups, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public Task<TaskGroup?> GetTaskGroupByIdAsync(Guid groupId)
        {
            lock (_lock)
            {
                var dto = ReadAll().FirstOrDefault(g => g.GroupId == groupId);
                var group = dto == null ? null : TaskGroupPersistenceMapper.ToDomain(dto);
                return Task.FromResult(group);
            }
        }

        public Task<IEnumerable<TaskGroup>> GetAllTaskGroupsAsync()
        {
            lock (_lock)
            {
                var groups = ReadAll().Select(TaskGroupPersistenceMapper.ToDomain).ToList();
                return Task.FromResult(groups.AsEnumerable());
            }
        }

        public Task AddTaskGroupAsync(TaskGroup taskGroup)
        {
            lock (_lock)
            {
                var groups = ReadAll();
                groups.Add(TaskGroupPersistenceMapper.ToDTO(taskGroup));
                WriteAll(groups);
            }
            return Task.CompletedTask;
        }

        public Task UpdateTaskGroupAsync(TaskGroup taskGroup)
        {
            lock (_lock)
            {
                var groups = ReadAll();
                var index = groups.FindIndex(g => g.GroupId == taskGroup.GroupId);
                if (index >= 0)
                    groups[index] = TaskGroupPersistenceMapper.ToDTO(taskGroup);
                else
                    groups.Add(TaskGroupPersistenceMapper.ToDTO(taskGroup));
                WriteAll(groups);
            }
            return Task.CompletedTask;
        }

        public Task DeleteTaskGroupAsync(Guid groupId)
        {
            lock (_lock)
            {
                var groups = ReadAll();
                groups.RemoveAll(g => g.GroupId == groupId);
                WriteAll(groups);
            }
            return Task.CompletedTask;
        }
    }
}