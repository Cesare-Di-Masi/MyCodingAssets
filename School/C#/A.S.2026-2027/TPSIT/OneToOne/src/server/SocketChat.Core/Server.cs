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

    //Classe interna
    private sealed class ClientSession
    {
        public ClientSession(string name, Socket socket)
        {
            Name = name;
            Socket = socket;
        }

        public string Name { get; }
        public Socket Socket { get; }
        public byte[] Buffer { get; } = new byte[BUFFER_SIZE];
        public object SendLock { get; } = new();
    }

    private readonly object sync = new();
    private readonly List<ClientSession> sessions = new();
    private Socket listener;
    private Action<string> messageCallback;

    public int Port { get; init; }
    public string HostName { get; init; }

    public bool IsClientConnected
    {
        get
        {
            lock (sync)
            {
                return sessions.Any(s => s.Socket.Connected);
            }
        }
    }

    public Server(int port, string hostName)
    {
        this.Port = port;
        this.HostName = hostName ?? DEFAULT_HOST_NAME;
    }

    // Configura l'ascolto e avvia subito l'accettazione dei client in background
    public void Start()
    {
        var hostIP = Dns.GetHostEntry(HostName).AddressList[0];
        var localEndpoint = new IPEndPoint(hostIP, Port);

        listener = new Socket(hostIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(localEndpoint);
        listener.Listen(MAX_CONNECTIONS);

        Task.Run(() =>
        {
            while (true) // Loop infinito per accettare nuovi client
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
        ClientSession session;
        lock (sync)
        {
            session = sessions.FirstOrDefault(s => s.Name == hostName);
        }

        return session is not null && TrySend(session, msg);
    }

    public bool TrySendMessageSingle(string msg, Socket client)
    {
        ClientSession session;
        lock (sync)
        {
            session = sessions.FirstOrDefault(s => s.Socket == client);
        }

        return session is not null && TrySend(session, msg);
    }

    public bool TrySendMessageBroadcast(string msg)
    {
        ClientSession[] snapshot = Snapshot();
        if (snapshot.Length == 0)
        {
            return false;
        }

        bool sentToAll = true;
        foreach (ClientSession session in snapshot)
        {
            if (!TrySend(session, msg))
            {
                sentToAll = false;
            }
        }

        return sentToAll;
    }

    // Ascolto in background speculare a quello del Client
    public void StartListening(Action<string> onMessageReceived)
    {
        ClientSession[] snapshot;
        lock (sync)
        {
            messageCallback = onMessageReceived;
            snapshot = sessions.ToArray();
        }

        // I client aggiunti da ora in poi vengono ascoltati direttamente da AddClient
        foreach (ClientSession session in snapshot)
        {
            Listen(session);
        }
    }

    private ClientSession[] Snapshot()
    {
        lock (sync)
        {
            return sessions.ToArray();
        }
    }

    private void AddClient(Socket client)
    {
        ClientSession session;
        bool listenNow;

        lock (sync)
        {
            // Il nome generato deve essere unico, altrimenti ci sarebbero collisioni
            string name;
            do
            {
                name = NameGenerator.Identifiers.Get(IdentifierTemplate.AnyTwoComponents, separator: "_", forceSingleLetter: true);
            }
            while (sessions.Any(s => s.Name == name));

            session = new ClientSession(name, client);
            sessions.Add(session);
            listenNow = messageCallback is not null;
        }

        TrySend(session, $"assign_identity: {session.Name}");

        if (listenNow)
        {
            Listen(session);
        }
    }

    private void Listen(ClientSession session)
    {
        Task.Run(() =>
        {
            try
            {
                while (session.Socket.Connected)
                {
                    int receivedBytes = session.Socket.Receive(session.Buffer);
                    if (receivedBytes == 0)
                    {
                        // Il client si e' disconnesso
                        break;
                    }

                    string decodedMessage = Encoding.UTF8.GetString(session.Buffer, 0, receivedBytes);

                    // Il client antepone "[nome]" al testo: i controlli vanno fatti sul testo senza il prefisso
                    string text = decodedMessage;
                    if (decodedMessage.StartsWith("["))
                    {
                        int closeBracketIndex = decodedMessage.IndexOf(']');
                        if (closeBracketIndex != -1)
                        {
                            text = decodedMessage.Substring(closeBracketIndex + 1);
                        }
                    }

                    if (text == "exit")
                    {
                        if (RemoveSession(session))
                        {
                            TrySendMessageBroadcast($"{session.Name} left from the session");
                        }
                        return;
                    }

                    if (text.EndsWith("!"))
                    {
                        decodedMessage = decodedMessage[..^1]; // toglie il "!" finale
                        ForwardMessage(decodedMessage, session);
                    }

                    messageCallback?.Invoke(decodedMessage);
                }
            }
            catch (SocketException)
            {
                // Connessione interrotta o socket chiuso
            }
            catch (ObjectDisposedException)
            {
                // Socket chiuso da un altro thread
            }

            DropClient(session);
        });
    }

    private void ForwardMessage(string message, ClientSession sender)
    {
        foreach (ClientSession session in Snapshot())
        {
            if (session == sender)
            {
                continue;
            }

            TrySend(session, message);
        }
    }

    // Invio con lock per-client; in caso di errore il client viene rimosso
    private bool TrySend(ClientSession session, string msg)
    {
        byte[] raw = Encoding.UTF8.GetBytes(msg);

        try
        {
            int sent;
            lock (session.SendLock)
            {
                sent = session.Socket.Send(raw);
            }
            return sent == raw.Length;
        }
        catch (SocketException)
        {
            DropClient(session);
            return false;
        }
        catch (ObjectDisposedException)
        {
            DropClient(session);
            return false;
        }
    }

    // Rimuove il client e annuncia la disconnessione solo se era ancora presente:
    // evita messaggi duplicati e cascate di broadcast
    private void DropClient(ClientSession session)
    {
        if (RemoveSession(session))
        {
            TrySendMessageBroadcast($"{session.Name} Disconnected from the session");
        }
    }

    private bool RemoveSession(ClientSession session)
    {
        bool removed;
        lock (sync)
        {
            removed = sessions.Remove(session);
        }

        try
        {
            session.Socket.Close();
        }
        catch (SocketException)
        {
        }
        catch (ObjectDisposedException)
        {
        }

        return removed;
    }

    public void Close()
    {
        ClientSession[] snapshot;
        lock (sync)
        {
            snapshot = sessions.ToArray();
            sessions.Clear();
        }

        foreach (ClientSession session in snapshot)
        {
            try
            {
                session.Socket.Close();
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        listener?.Close();
    }
}