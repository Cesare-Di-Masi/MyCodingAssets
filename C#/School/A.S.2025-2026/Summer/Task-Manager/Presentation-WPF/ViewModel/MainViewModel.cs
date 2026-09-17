using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Application.DTO;
using Application.Interface;
using Domain.Enum;

namespace Presentation_WPF.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ITasksService _tasksService;
        private bool _isRunning;

        private string _newTaskName = string.Empty;
        private string _newTaskDurationSeconds = string.Empty;
        private Guid? _newTaskGroupId;
        private string _newGroupName = string.Empty;
        private string _statusMessage = string.Empty;

        public ObservableCollection<ReadTaskDTO> Tasks { get; } = new();
        public ObservableCollection<ReadTaskGroupDTO> TaskGroups { get; } = new();

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    (StartAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (StartGroupCommand as RelayCommand<Guid>)?.RaiseCanExecuteChanged();
                }
            }
        }

        public int CompletedCount => Tasks.Count(t => t.State == TaskState.Completed);
        public int TotalCount => Tasks.Count;
        public double ProgressPercent => TotalCount == 0 ? 0 : (double)CompletedCount / TotalCount * 100;

        public string NewTaskName { get => _newTaskName; set => SetProperty(ref _newTaskName, value); }
        public string NewTaskDurationSeconds { get => _newTaskDurationSeconds; set => SetProperty(ref _newTaskDurationSeconds, value); }
        public Guid? NewTaskGroupId { get => _newTaskGroupId; set => SetProperty(ref _newTaskGroupId, value); }
        public string NewGroupName { get => _newGroupName; set => SetProperty(ref _newGroupName, value); }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand StartAllCommand { get; }
        public ICommand AddTaskCommand { get; }
        public ICommand AddGroupCommand { get; }
        public ICommand StartGroupCommand { get; }

        public MainViewModel(ITasksService tasksService)
        {
            _tasksService = tasksService;

            StartAllCommand = new RelayCommand(
                async () => await StartAllAsync(),
                () => !IsRunning && Tasks.Any(t => t.State == TaskState.Waiting)
            );
            AddTaskCommand = new RelayCommand(async () => await AddTaskAsync());
            AddGroupCommand = new RelayCommand(async () => await AddGroupAsync());
            StartGroupCommand = new RelayCommand<Guid>(
                async (id) => await StartGroupAsync(id),
                _ => !IsRunning
            );

            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            await RefreshTasksAsync();
            await RefreshTaskGroupsAsync();
        }

        private async Task StartAllAsync()
        {
            IsRunning = true;
            try
            {
                await _tasksService.StartAllTasksAsync(OnTaskStateChanged);
                await RefreshTaskGroupsAsync();
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsRunning = false; }
        }

        private async Task StartGroupAsync(Guid groupId)
        {
            IsRunning = true;
            try
            {
                await _tasksService.StartTaskGroupAsync(groupId, OnTaskStateChanged);
                await RefreshTaskGroupsAsync();
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsRunning = false; }
        }

        private async Task AddTaskAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTaskName) || !double.TryParse(NewTaskDurationSeconds, out var duration) || duration <= 0)
            {
                StatusMessage = "Inserisci nome e durata validi (secondi > 0).";
                return;
            }

            try
            {
                await _tasksService.CreateTaskAsync(new CreateTaskDTO
                {
                    Name = NewTaskName,
                    EstimatedTimeSeconds = duration,
                    GroupId = NewTaskGroupId
                });
                NewTaskName = string.Empty;
                NewTaskDurationSeconds = string.Empty;
                NewTaskGroupId = null;
                StatusMessage = string.Empty;
                await RefreshTasksAsync();
                await RefreshTaskGroupsAsync();
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
        }

        private async Task AddGroupAsync()
        {
            if (string.IsNullOrWhiteSpace(NewGroupName))
            {
                StatusMessage = "Inserisci un nome per il gruppo.";
                return;
            }

            try
            {
                await _tasksService.CreateTaskGroupAsync(new CreateTaskGroupDTO { GroupName = NewGroupName });
                NewGroupName = string.Empty;
                StatusMessage = string.Empty;
                await RefreshTaskGroupsAsync();
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
        }

        private void OnTaskStateChanged(ReadTaskDTO updated)
        {
            // Update standalone task
            var existing = Tasks.FirstOrDefault(t => t.Id == updated.Id);
            if (existing != null)
            {
                var index = Tasks.IndexOf(existing);
                Tasks[index] = updated;
            }

            // Update task inside groups
            foreach (var group in TaskGroups)
            {
                var taskInGroup = group.Tasks.FirstOrDefault(t => t.Id == updated.Id);
                if (taskInGroup != null)
                {
                    var idx = group.Tasks.IndexOf(taskInGroup);
                    group.Tasks[idx] = updated;
                }
            }

            // Refresh progress stats
            OnPropertyChanged(nameof(CompletedCount));
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(ProgressPercent));

            // Update command availability (when tasks finish, button may become enabled again)
            (StartAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private async Task RefreshTasksAsync()
        {
            var tasks = await _tasksService.GetAllTasksAsync();
            Tasks.Clear();
            foreach (var t in tasks) Tasks.Add(t);
            OnPropertyChanged(nameof(CompletedCount));
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(ProgressPercent));
            (StartAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private async Task RefreshTaskGroupsAsync()
        {
            var groups = await _tasksService.GetAllTaskGroupsAsync();
            TaskGroups.Clear();
            foreach (var g in groups) TaskGroups.Add(g);
        }
    }
}