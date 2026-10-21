namespace SocketChat.GUI;
using SocketChat.Core;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = args[0] ?? "localhost";
        var port = int.Parse(args[1] ?? "11000");

        var client = new Client(host, port);
        client.Connect();

        if(!client.IsConnected)
        {
            Console.Error.WriteLine("Connection unsuccesful!");
            Environment.Exit(1);
        }

        client.StartListening((message) => Console.WriteLine("[Server] " + message));

        bool keepGoing = true;

        while(keepGoing && client.IsConnected)
        {
            var message = Console.ReadLine();
            if(!string.IsNullOrEmpty(message))
            {
                bool sent = client.TrySendMessage(message).Result;
                if(sent)
                {
                    Console.WriteLine("[Client] " + message);
                }

                keepGoing = message!= "exit";
            }
        }

    }
}
