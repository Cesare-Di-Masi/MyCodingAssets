using System;
using System.IO;
using System.Threading.Tasks;
using Application.DTO;
using Application.Interface;
using Application.Service;
using Infrastructure.Repository;

namespace Console_Seeder
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            // Parse command line or ask interactively
            int standaloneCount = GetArg(args, 0, 5);
            int groupCount = GetArg(args, 1, 2);
            int tasksPerGroup = GetArg(args, 2, 3);

            if (standaloneCount < 0 || groupCount < 0 || tasksPerGroup < 0)
            {
                Console.WriteLine("All counts must be non‑negative.");
                return;
            }

            // Set up data directory
            var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TaskManager");
            Directory.CreateDirectory(dataDir);

            var tasksPath = Path.Combine(dataDir, "tasks.json");
            var groupsPath = Path.Combine(dataDir, "taskgroups.json");

            // Delete old data
            if (File.Exists(tasksPath)) File.Delete(tasksPath);
            if (File.Exists(groupsPath)) File.Delete(groupsPath);

            // Build repositories and service
            var taskRepo = new JsonTaskRepository(tasksPath);
            var groupRepo = new JsonTaskGroupRepository(groupsPath);
            ITasksService service = new TasksService(taskRepo, groupRepo);

            var random = new Random();
            int totalCreated = 0;

            Console.WriteLine($"Seeding {standaloneCount} standalone tasks, {groupCount} groups with {tasksPerGroup} tasks each...");

            // Create standalone tasks
            for (int i = 1; i <= standaloneCount; i++)
            {
                await service.CreateTaskAsync(new CreateTaskDTO
                {
                    Name = $"Task {i}",
                    EstimatedTimeSeconds = random.Next(1, 10)
                });
                totalCreated++;
                Console.Write(".");
                if (i % 20 == 0) Console.WriteLine($" {i} tasks done");
            }

            // Create groups
            for (int g = 1; g <= groupCount; g++)
            {
                var group = await service.CreateTaskGroupAsync(new CreateTaskGroupDTO
                {
                    GroupName = $"Group {g}"
                });
                totalCreated++;

                for (int t = 1; t <= tasksPerGroup; t++)
                {
                    await service.CreateTaskAsync(new CreateTaskDTO
                    {
                        Name = $"Group {g} - Task {t}",
                        EstimatedTimeSeconds = random.Next(1, 10),
                        GroupId = group.GroupId
                    });
                    totalCreated++;
                    Console.Write(".");
                    if (t % 20 == 0) Console.WriteLine($"   Group {g}: {t} tasks done");
                }
            }

            Console.WriteLine($"\n✅ Seeding complete. Created {totalCreated} tasks total.");
            Console.WriteLine($"Data stored at: {dataDir}");
        }

        private static int GetArg(string[] args, int index, int defaultValue)
        {
            if (args.Length > index && int.TryParse(args[index], out int value))
                return value >= 0 ? value : -1;

            // Interactive fallback
            string prompt = index switch
            {
                0 => "How many standalone tasks?",
                1 => "How many groups?",
                2 => "How many tasks per group?",
                _ => "Enter number:"
            };
            Console.Write($"{prompt} (default {defaultValue}): ");
            string input = Console.ReadLine() ?? "";
            if (string.IsNullOrWhiteSpace(input))
                return defaultValue;
            return int.TryParse(input, out int result) && result >= 0 ? result : -1;
        }
    }
}