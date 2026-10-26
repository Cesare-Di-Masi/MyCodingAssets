using PriorityQueue;

ActivityManager activityManager = new ActivityManager();
PriorityQueue<Activity, int> priorityQueue = new PriorityQueue<Activity, int>(activityManager);

Activity activity1 = new Activity("Activity 1", 2, 1);
Activity activity2 = new Activity("Activity 2", 1, 1);
Activity activity3 = new Activity("Activity 3", 3, 1);
priorityQueue.Enqueue(activity1, activity1.Priority);
priorityQueue.Enqueue(activity2, activity2.Priority);
priorityQueue.Enqueue(activity3, activity3.Priority);
while (priorityQueue.Count > 0)
{
    Activity activity = priorityQueue.Dequeue();
    Console.WriteLine($"Processing {activity.ID} with Priority {activity.Priority}");
}