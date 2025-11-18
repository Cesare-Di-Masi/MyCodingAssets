// See https://aka.ms/new-console-template for more information
using Queue_Implementation;

Console.WriteLine("Hello, World!");
MyQueue queue = new MyQueue();
queue.Enqueue(1);
queue.Enqueue(2);
queue.Enqueue(3);
Console.WriteLine($"Dequeued: {queue.Dequeue()}"); // Outputs: Dequeued: 1