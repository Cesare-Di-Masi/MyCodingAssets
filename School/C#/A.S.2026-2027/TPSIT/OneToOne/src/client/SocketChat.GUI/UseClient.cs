using SocketChat.Core;

namespace SocketChat.GUI;

public class UseClient
{
    private static void Fatal(string message)
    {
        Console.Error.WriteLine(message);
        Environment.Exit(1);
    }

    public static void Main(string[] args)
    {
        var host = args.ElementAtOrDefault(0) ?? "localhost";
        if (!int.TryParse(args.ElementAtOrDefault(1) ?? "11000", out int port))
        {
            Fatal($"Invalid port: {args[1]}");
        }

        var client = new Client(port, host); 
        client.Connect();

        if (!client.IsConnected)
        {
            Fatal($"Could not connect to {host}:{port}");
        }

        client.StartListening(message =>
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"[Server] ");
            Console.ResetColor();
            Console.WriteLine(message);
        });

        bool keepGoing = true;
        Console.WriteLine("Type here your messages and press ENTER to send them!");
        while (keepGoing && client.IsConnected)
        {
            var message = Console.ReadLine();
            if (!string.IsNullOrEmpty(message))
            {
                bool sent = client.TrySendMessage(message);
                if (sent)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"[{client.AssignedName}] ");
                    Console.ResetColor();
                    Console.WriteLine(message);
                }
            }
            keepGoing = message != "exit";
        }

        client.Close();
    }
}