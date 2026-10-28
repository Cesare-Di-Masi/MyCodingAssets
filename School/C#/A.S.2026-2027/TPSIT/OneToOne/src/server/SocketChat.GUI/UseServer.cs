using SocketChat.Core;

namespace SocketChat.GUI;

public class UseServer
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

        var server = new Server(port, host);

        try
        {
            server.Start();
        }
        catch (Exception ex) when (ex is System.Net.Sockets.SocketException)
        {
            Fatal($"Could not start the server on {host}:{port}");
        }

        server.StartListening(message =>
        {
            int closeBracketIndex = message.IndexOf(']');

            if (closeBracketIndex != -1)
            {
                // Estrae "[nome client]"
                string clientTag = message.Substring(0, closeBracketIndex + 1);

                // Estrae il resto del messaggio " mesg"
                string textMessage = message.Substring(closeBracketIndex + 1);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(clientTag+ " ");
                Console.ResetColor();
                Console.WriteLine(textMessage);
            }
            else
            {
                // Fallback se il messaggio non segue il formato atteso
                Console.WriteLine(message);
            }
        });


        bool keepGoing = true;
        Console.WriteLine("Type here your message and press ENTER to send it!");
        // Start() non blocca piu' in attesa del primo client, quindi il ciclo non dipende da IsClientConnected
        while (keepGoing)
        {
            var message = Console.ReadLine();
            if (!string.IsNullOrEmpty(message))
            {
                bool sent = server.TrySendMessageBroadcast(message);
                if (sent)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("[Server] ");
                    Console.ResetColor();
                    Console.WriteLine(message);
                }
            }
            keepGoing = message != "exit";
        }

        server.Close();
    }
}