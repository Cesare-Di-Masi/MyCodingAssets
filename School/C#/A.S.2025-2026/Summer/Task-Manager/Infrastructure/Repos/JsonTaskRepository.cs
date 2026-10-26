using Application.Interface;
using Domain.Entity;
using Infrastructure.Persistence.DTO;
using Infrastructure.Persistence.Mapper;
using System.Text.Json;

namespace Infrastructure.Repository
{
    public class JsonTaskRepository : ITaskRepository
    {
        private readonly string _filePath;
        private readonly object _lock = new();

        public JsonTaskRepository(string filePath)
        {
            _filePath = filePath;
        }

        private List<TaskPersistenceDTO> ReadAll()
        {
            if (!File.Exists(_filePath))
                return new List<TaskPersistenceDTO>();

            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
                return new List<TaskPersistenceDTO>();

            return JsonSerializer.Deserialize<List<TaskPersistenceDTO>>(json) ?? new List<TaskPersistenceDTO>();
        }

        private void WriteAll(List<TaskPersistenceDTO> tasks)
        {
            var json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public Task<Tasks?> GetTaskByIdAsync(Guid taskId)
        {
            lock (_lock)
            {
                var dto = ReadAll().FirstOrDefault(t => t.Id == taskId);
                var task = dto == null ? null : TaskPersistenceMapper.ToDomain(dto);
                return Task.FromResult(task);
            }
        }

        public Task<IEnumerable<Tasks>> GetAllTasksAsync()
        {
            lock (_lock)
            {
                var tasks = ReadAll().Select(TaskPersistenceMapper.ToDomain).ToList();
                return Task.FromResult(tasks.AsEnumerable());
            }
        }

        public Task AddTaskAsync(Tasks task)
        {
            lock (_lock)
            {
                var tasks = ReadAll();
                tasks.Add(TaskPersistenceMapper.ToDTO(task));
                WriteAll(tasks);
            }
            return Task.CompletedTask;
        }

        public Task UpdateTaskAsync(Tasks task)
        {
            lock (_lock)
            {
                var tasks = ReadAll();
                var index = tasks.FindIndex(t => t.Id == task.Id);
                if (index >= 0)
                    tasks[index] = TaskPersistenceMapper.ToDTO(task);
                else
                    tasks.Add(TaskPersistenceMapper.ToDTO(task));
                WriteAll(tasks);
            }
            return Task.CompletedTask;
        }

        public Task DeleteTaskAsync(Guid taskId)
        {
            lock (_lock)
            {
                var tasks = ReadAll();
                tasks.RemoveAll(t => t.Id == taskId);
                WriteAll(tasks);
            }
            return Task.CompletedTask;
        }
    }
}