using Application.Service;
using Infrastructure.Repository;
using Presentation_WPF.ViewModel;

using System.Windows;

namespace Presentation_WPF
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Data directory
            var dataDir = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                "TaskManager");
            System.IO.Directory.CreateDirectory(dataDir);

            var taskRepo = new JsonTaskRepository(System.IO.Path.Combine(dataDir, "tasks.json"));
            var groupRepo = new JsonTaskGroupRepository(System.IO.Path.Combine(dataDir, "taskgroups.json"));

            // Build services
            var executionService = new TaskExecutionService();
            var orchestrationService = new TaskOrchestrationService(executionService);
            var tasksService = new TasksService(taskRepo, groupRepo, orchestrationService);

            // Create and show main window
            var mainWindow = new MainWindow();
            mainWindow.DataContext = new MainViewModel(tasksService);
            mainWindow.Show();
        }
    }
}