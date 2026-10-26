namespace SocketChat.Core;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using Bogus;

public class Server
{
    private const string DEFAULT_HOST_NAME = "localhost";
    private const int BUFFER_SIZE = 1024;
    private const int MAX_CONNECTIONS = 10;

    private List<string> ClientsName = new();
    Faker faker = new Faker("it");
    private Socket listener;
    private readonly List<Socket> clientHandlers = new();
    private readonly List<byte[]> buffers = new();
    private Action<string> messageCallback;

    public int Port { get; init; }
    public string HostName { get; init; }
    public bool IsClientConnected => clientHandlers.Exists(client => client.Connected);

    public Server(int port, string hostName)
    {
        this.Port = port;
        this.HostName = hostName ?? DEFAULT_HOST_NAME;
    }

    private byte[] ClearBuffer()
    {
        return new byte[BUFFER_SIZE];
    }

    // Configura l'ascolto e attende l'arrivo di un client
    public void Start()
    {
        var hostIP = Dns.GetHostEntry(HostName).AddressList[0];
        var localEndpoint = new IPEndPoint(hostIP, Port);

        listener = new Socket(hostIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(localEndpoint);
        listener.Listen(MAX_CONNECTIONS);

        // Blocca finché un client non si connette
        AddClient(listener.Accept());

        Task.Run(() =>
        {
            while (true) //Loop infinito per accettare nuovi client
            {
                try
                {
                    AddClient(listener.Accept());
                }
                catch (SocketException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
            }
        });
    }

    public bool TrySendMessage(string msg)
    {
        if (clientHandlers.Count == 0)
        {
            return false;
        }

        byte[] raw = Encoding.UTF8.GetBytes(msg);
        bool sentToAll = true;
        foreach (Socket clientHandler in clientHandlers.ToArray())
        {
        try{
            int sent = clientHandler.Send(raw);
            if (sent != raw.Length)
            {
                sentToAll = false;
            }
        }
        catch (SocketException)
        {
                sentToAll = false;
                RemoveClient(clientHandler);
            }
        }

        return sentToAll;
    }

    // Ascolto in background speculare a quello del Client
    public void StartListening(Action<string> onMessageReceived)
    {
        messageCallback = onMessageReceived;
        for (int i = 0; i < clientHandlers.Count; i++)
        {
            StartListening(clientHandlers[i], buffers[i]);
        }
    }

    private void AddClient(Socket clientHandler)
    {
        clientHandlers.Add(clientHandler);
        buffers.Add(ClearBuffer());
        ClientsName.Add(faker.Name.FullName());
        if (messageCallback is not null)
        {
            StartListening(clientHandler, buffers[buffers.Count - 1]);
        }
        TrySendMessage($"{ClientsName[clientHandlers.Count - 1]} si è connesso al server!");
    }

    private void StartListening(Socket clientHandler, byte[] buffer)
    {
        Task.Run(() =>
        {
            while (clientHandler.Connected)
            {
                try
                {
                    int receivedBytes = clientHandler.Receive(buffer);
                    if (receivedBytes == 0)
                    {
                        // Il client si è disconnesso
                        break;
                    }

                    string decodedMessage = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                    messageCallback(decodedMessage);
                }
                catch (SocketException)
                {
                    // Connessione interrotta o socket chiuso
                    break;
                }
            }

            RemoveClient(clientHandler);
        });
    }

    private void ForwardMessage(string message, Socket sender)
    {
        byte[] raw = Encoding.UTF8.GetBytes(message);
        foreach (Socket client in clientHandlers.ToArray())
        {
            if (client == sender)
            {
                continue;
            }

            try
            {
                client.Send(raw);
            }
            catch (SocketException)
            {
                RemoveClient(client);
            }
        }
    }

    private void RemoveClient(Socket clientHandler)
    {
        int index = clientHandlers.IndexOf(clientHandler);
        if (index >= 0)
        {
            clientHandlers.RemoveAt(index);
            buffers.RemoveAt(index);
        }

        clientHandler.Close();
    }

    public void Close()
    {
        foreach (Socket clientHandler in clientHandlers.ToArray())
        {
            clientHandler.Close();
        }

        listener?.Close();
    }
}