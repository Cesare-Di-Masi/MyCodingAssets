
using SocketChat.Core;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace SocketChat.GUI;

public class UseServer
{
    private static Server? server;
    private static bool running = true;

    private static void WriteHeader(string host, int port)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==================================================");
        Console.WriteLine("                 SOCKETCHAT SERVER                ");
        Console.WriteLine("==================================================");
        Console.ResetColor();

        Console.WriteLine($" Host       : {host}");
        Console.WriteLine($" Port       : {port}");
        Console.WriteLine($" Protocol   : TCP");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" Status     : ONLINE");
        Console.ResetColor();

        Console.WriteLine("==================================================");
        Console.WriteLine(" Commands: help | status | clear | exit");
        Console.WriteLine("==================================================\n");
    }

    private static void WriteLog(string message)
    {
        lock (Console.Out)
        {
            int closeBracketIndex = message.IndexOf(']');

            if (closeBracketIndex != -1)
            {
                string clientTag = message.Substring(0, closeBracketIndex + 1);
                string textMessage = message.Substring(closeBracketIndex + 1);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(clientTag);
                Console.ResetColor();

                Console.WriteLine(textMessage);
            }
            else
            {
                Console.WriteLine(message);
            }

            Console.Write("> ");
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("\nAvailable commands:");
        Console.WriteLine(" help    - Show available commands");
        Console.WriteLine(" status  - Show server status");
        Console.WriteLine(" clear   - Clear console");
        Console.WriteLine(" exit    - Stop server\n");
    }

    private static void ShowStatus(string host, int port)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n[ONLINE] SocketChat Server is running.");
        Console.ResetColor();

        Console.WriteLine($"Host: {host}");
        Console.WriteLine($"Port: {port}");
        Console.WriteLine($"Protocol: TCP\n");
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

        server = new Server(port, host);

        try
        {
            server.Start();
        }
        catch (SocketException ex)
        {
            Fatal($"Could not start server on {host}:{port}. {ex.Message}");
            return;
        }

        WriteHeader(host, port);

        server.StartListening(WriteLog);

        Console.WriteLine("Server ready. Waiting for connections...\n");

        while (running)
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
                    bool sent = server.TrySendMessageBroadcast(message);

                    if (sent)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("[Server] ");
                        Console.ResetColor();
                        Console.WriteLine(message);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[ERROR] Unable to broadcast message.");
                        Console.ResetColor();
                    }

                    break;
            }
        }

        server.Close();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n[OFFLINE] SocketChat Server stopped.");
        Console.ResetColor();
    }
}
