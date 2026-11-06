
using SocketChat.Core;
using System;
using System.Linq;
using System.Net.Sockets;

namespace SocketChat.GUI;

public class UseClient
{
    private static Client? client;
    private static bool running = true;

    private static void WriteHeader(string host, int port)
    {
        Console.Clear();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================");
        Console.WriteLine("                  SOCKETCHAT CLIENT               ");
        Console.WriteLine("==================================================");
        Console.ResetColor();

        Console.WriteLine($" Server     : {host}");
        Console.WriteLine($" Port       : {port}");
        Console.WriteLine($" Protocol   : TCP");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" Status     : CONNECTED");
        Console.ResetColor();

        Console.WriteLine($" Identity   : {client?.AssignedName ?? "Unknown"}");

        Console.WriteLine("==================================================");
        Console.WriteLine(" Commands: help | status | clear | exit");
        Console.WriteLine("==================================================\n");
    }

    private static void WriteLog(string message)
    {
        lock (Console.Out)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[Server] ");
            Console.ResetColor();

            Console.WriteLine(message);
            Console.Write("> ");
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("\nAvailable commands:");
        Console.WriteLine(" help    - Show available commands");
        Console.WriteLine(" status  - Show connection status");
        Console.WriteLine(" clear   - Clear console");
        Console.WriteLine(" exit    - Disconnect from server\n");
    }

    private static void ShowStatus(string host, int port)
    {
        Console.WriteLine();

        if (client?.IsConnected == true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[CONNECTED] Connection is active.");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[DISCONNECTED] Connection is not active.");
        }

        Console.ResetColor();

        Console.WriteLine($"Server: {host}");
        Console.WriteLine($"Port: {port}");
        Console.WriteLine($"Identity: {client?.AssignedName ?? "Unknown"}\n");
    }

    private static void Fatal(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"\n[FATAL] {message}");
        Console.ResetColor();

        Environment.Exit(1);
    }

    public static void Main(string[] args)
    {
        string host = args.ElementAtOrDefault(0) ?? "localhost";

        if (!int.TryParse(args.ElementAtOrDefault(1) ?? "11000", out int port)
            || port < 1 || port > 65535)
        {
            Fatal("Invalid port. Use a value between 1 and 65535.");
            return;
        }

        Console.WriteLine("Connecting to SocketChat...");
        Console.WriteLine($"Target: {host}:{port}\n");

        client = new Client(port, host);

        try
        {
            client.Connect();
        }
        catch (SocketException ex)
        {
            Fatal($"Connection failed: {ex.Message}");
            return;
        }
        catch (Exception ex)
        {
            Fatal($"Unexpected error: {ex.Message}");
            return;
        }

        if (!client.IsConnected)
            Fatal($"Could not connect to {host}:{port}");

        WriteHeader(host, port);

        client.StartListening(WriteLog);

        Console.WriteLine("Connected successfully.");
        Console.WriteLine("You can now send messages.\n");

        while (running && client.IsConnected)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("> ");
            Console.ResetColor();

            string? message = Console.ReadLine();

            if (message == null)
                break;

            message = message.Trim();

            if (string.IsNullOrEmpty(message))
                continue;

            switch (message.ToLowerInvariant())
            {
                case "help":
                    ShowHelp();
                    break;

                case "status":
                    ShowStatus(host, port);
                    break;

                case "clear":
                    WriteHeader(host, port);
                    break;

                case "exit":
                    running = false;
                    break;

                default:
                    bool sent = client.TrySendMessage(message);

                    if (sent)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"[{client.AssignedName}] ");
                        Console.ResetColor();
                        Console.WriteLine(message);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[ERROR] Message could not be sent.");
                        Console.ResetColor();
                    }

                    break;
            }
        }

        client.Close();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n[DISCONNECTED] Connection closed.");
        Console.ResetColor();
    }
}
