namespace SocketChat.Core;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using RandomFriendlyNameGenerator;

public class Server
{
    private const string DEFAULT_HOST_NAME = "localhost";
    private const int BUFFER_SIZE = 1024;
    private const int MAX_CONNECTIONS = 10;

    private List<string> clientsName = new();
    private Socket listener;
    private readonly Dictionary<string,Socket> clientHandlers = new();
    private readonly List<byte[]> buffers = new();
    private Action<string> messageCallback;

    public int Port { get; init; }
    public string HostName { get; init; }
    
    public bool IsClientConnected =>
    clientHandlers.Values.Any(client => client.Connected);
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

    public bool TrySendMessageSingle(string msg, string hostName)
    {
        bool sent = false;

        byte[] raw = Encoding.UTF8.GetBytes(msg);

        try
        {
            int sentdata = clientHandlers[hostName].Send(raw);
            if (sentdata != raw.Length)
            {
                sent = false;
            }
        }
        catch (SocketException)
        {
            sent = false;
            Socket client = clientHandlers[hostName];
            RemoveClient(client);
            TrySendMessageBroadcast($"{hostName} Disconnected from the session");
        }

        return sent;
    }

    public bool TrySendMessageSingle(string msg, Socket client)
    {
        bool sent = false;

        byte[] raw = Encoding.UTF8.GetBytes(msg);

        try
        {
            int sentdata = client.Send(raw);
            if (sentdata != raw.Length)
            {
                sent = false;
            }
        }
        catch (SocketException)
        {
            sent = false;
            string clientName = getClientName(client);
            RemoveClient(client);
            TrySendMessageBroadcast($"{clientName} Disconnected from the session");
        }

        return sent;
    }

    public bool TrySendMessageBroadcast(string msg)
    {
        if (clientHandlers.Count == 0)
        {
            return false;
        }

        byte[] raw = Encoding.UTF8.GetBytes(msg);
        bool sentToAll = true;
        foreach (Socket client in clientHandlers.Values.ToArray())
        {
            try
            {
                int sent = client.Send(raw);
                if (sent != raw.Length)
                {
                    sentToAll = false;
                }
            }
            catch (SocketException)
            {
                sentToAll = false;
                string clientName = getClientName(client);
                RemoveClient(client);
                TrySendMessageBroadcast($"{clientName} Disconnected from the session");
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
            StartListening(clientHandlers.Values.ToArray()[i], buffers[i]);
        }
    }

    private void AddClient(Socket client)
    {
        buffers.Add(ClearBuffer());
        if (messageCallback is not null)
        {
            StartListening(client, buffers[buffers.Count - 1]);
        }
        AssignClientName(client);
    }

    private void StartListening(Socket client, byte[] buffer)
    {
        Task.Run(() =>
        {
            while (client.Connected)
            {
                try
                {
                    int receivedBytes = client.Receive(buffer);
                    if (receivedBytes == 0)
                    {
                        // Il client si è disconnesso
                        break;
                    }

                    string decodedMessage = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                    
                    if(decodedMessage == "exit")
                    {
                        string clientName = getClientName(client);
                        RemoveClient(client);
                        TrySendMessageBroadcast($"{clientName} left from the session");
                        return;
                    }
                    
                    if(decodedMessage.EndsWith("!"))
                    {
                        decodedMessage = decodedMessage[..^1]; //toglie l'ultimo carattere della stringa cha sarà !
                        ForwardMessage(decodedMessage, client);
                    }

                    messageCallback(decodedMessage);
                }
                catch (SocketException)
                {
                    // Connessione interrotta o socket chiuso
                    break;
                }
            }
            string disconnectedClientName = getClientName(client);
            RemoveClient(client);
            TrySendMessageBroadcast($"{disconnectedClientName} Disconnected from the session");
        });
    }

    private void ForwardMessage(string message, Socket sender)
    {
        byte[] raw = Encoding.UTF8.GetBytes(message);
        foreach (Socket client in clientHandlers.Values.ToArray())
        {
            if (client == sender)
            {
                continue;
            }

            try
            {
                TrySendMessageSingle(message, client);
            }
            catch (SocketException)
            {
                string clientName = getClientName(client);
                RemoveClient(client);
                TrySendMessageBroadcast($"{clientName} Disconnected from the session");
            }
        }
    }

    private void RemoveClient(Socket client)
    {
        int index = clientHandlers.Values.ToArray().IndexOf(client);

        string key = clientHandlers.FirstOrDefault(x => x.Value == client).Key;

        if (index >= 0 && key != null)
        {
            clientHandlers.Remove(key);
            buffers.RemoveAt(index);
            clientsName.RemoveAt(index);
        }

        client.Close();
    }

    private void AssignClientName(Socket client)
    {
        string generatedName = NameGenerator.Identifiers.Get(IdentifierTemplate.AnyTwoComponents, separator:"_", forceSingleLetter:true);
        clientsName.Add(generatedName);
        clientHandlers.Add(generatedName, client);
        TrySendMessageSingle($"assign_identity: {generatedName}", client);
    }

    private string getClientName(Socket client)
    {
        string name = clientHandlers.FirstOrDefault(x => x.Value == client).Key ?? "Unknown";
        return name;
    }

    public void Close()
    {
        foreach (Socket client in clientHandlers.Values.ToArray())
        {
            client.Close();
        }

        listener?.Close();
    }
}