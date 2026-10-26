using FistAidQueue;
using System;

EmergencyComparer emergencyComparer = new EmergencyComparer();

PriorityQueue<Patient, int> priorityQueue = new PriorityQueue<Patient, int>(emergencyComparer);
Patient p1 = new Patient("John Doe", 30, EmergencyLevel.Red);
Patient p2 = new Patient("Jane Smith", 25, EmergencyLevel.Yellow);
Patient p3 = new Patient("Alice Johnson", 40, EmergencyLevel.Green);

priorityQueue.Enqueue(p1, (int)p1.EmergencyLevel);
priorityQueue.Enqueue(p2, (int)p2.EmergencyLevel);
priorityQueue.Enqueue(p3, (int)p3.EmergencyLevel);

while (priorityQueue.Count > 0)
{
    Patient nextPatient = priorityQueue.Dequeue();
    Console.WriteLine($"Attending to patient: {nextPatient.Name}, Age: {nextPatient.Age}, Emergency Level: {nextPatient.EmergencyLevel}");
}