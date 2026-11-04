using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using RandomFriendlyNameGenerator;

namespace SocketChat.Core;

public class Server
{
    private const string DEFAULT_HOST_NAME = "localhost";
    private const int DEFAULT_PORT = 11000;
    private const int BUFFER_SIZE = 1024;
    private const int MAX_CONNECTIONS = 10;

    // Comandi inviati dal client dopo il prefisso "[nome]"
    private static readonly string[] EXIT_COMMAND = { "exit", "quit", "close" };
    private static readonly string[] BROADCAST_PREFIX = { "!", "/broadcast", "/b" };
    private static readonly string[] PrivateCommands = { "/w", "/t", "/msg", "/whisper", "/tell", "/private" };
    private static readonly string[] GroupCommands = { "/g", "/group" };
    private static readonly string[] SnapshotCommands = { "/list", "/who", "/snapshot", "/all"};

    // Raggruppa socket, buffer e lock di invio di un singolo client
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

    // I gruppi sono canali creati dal server: i client al loro interno scrivono tra di loro
    private sealed class Groups
    {
        public Groups(string name, List<Socket> clients)
        {
            Name = name;
            Clients = clients;
        }

        public string Name { get; }
        public List<Socket> Clients { get; }

        public void AddClient(Socket client) => Clients.Add(client);
        public void RemoveClient(Socket client) => Clients.Remove(client);
    }

    // Lock per le operazioni concorrenti su "sessions" e "messageCallback"
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

    public Server(int? port, string? hostName)
    {
        Port = port ?? DEFAULT_PORT;
        HostName = hostName ?? DEFAULT_HOST_NAME;
    }

    // Configura l'ascolto e avvia subito l'accettazione dei client in background
    public void Start()
    {
        var hostIP = Dns.GetHostEntry(HostName).AddressList[0];

        listener = new Socket(hostIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(hostIP, Port));
        listener.Listen(MAX_CONNECTIONS);

        Task.Run(AcceptLoop);
    }

    public bool TrySendMessageSingle(string msg, string clientName)
    {
        ClientSession session = FindSession(s => s.Name == clientName);
        return session is not null && TrySend(session, msg);
    }

    public bool TrySendMessageSingle(string msg, Socket client)
    {
        ClientSession session = FindSession(s => s.Socket == client);
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
            sentToAll &= TrySend(session, msg); //per quanti client ci siano, se uno non riceve il messaggio, sentToAll diventa false --> operatore AND &=
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
            CloseSocketQuietly(session.Socket);
        }

        listener?.Close();
    }

    private void AcceptLoop()
    {
        while (true)
        {
            try
            {
                AddClient(listener.Accept());
            }
            catch (Exception ex) when (IsSocketClosed(ex))
            {
                return; // Listener chiuso: fine dell'accettazione
            }
        }
    }

    private void AddClient(Socket client)
    {
        ClientSession session;
        bool listenNow;

        lock (sync)
        {
            session = new ClientSession(GenerateUniqueName(), client);
            sessions.Add(session);
            listenNow = messageCallback is not null;
        }

        Log($"Client connesso: {session.Name}");
        TrySend(session, $"assign_identity: {session.Name}"); // assegna il nominativo al client

        if (listenNow)
        {
            Listen(session);
        }
    }

    // Da chiamare dentro il lock: il nome deve essere unico (sempre in minuscolo)
    private string GenerateUniqueName()
    {
        string name;
        do
        {
            name = NameGenerator.Identifiers
                .Get(IdentifierTemplate.AnyTwoComponents, separator: "_", forceSingleLetter: true)
                .ToLowerInvariant();
        }
        while (sessions.Any(s => s.Name == name));

        return name;
    }

    // Rimuove il client e annuncia la disconnessione solo se era ancora presente:
    private void DropClient(ClientSession session)
    {
        if (RemoveSession(session))
        {
            Log($"Client disconnesso: {session.Name}");
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

        CloseSocketQuietly(session.Socket);
        return removed;
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
                        Log($"{session.Name} ha chiuso la connessione");
                        break;
                    }

                    string rawMessage = Encoding.UTF8.GetString(session.Buffer, 0, receivedBytes);
                    string text = StripClientPrefix(rawMessage);

                    if (EXIT_COMMAND.Contains(text))
                    {
                        HandleExit(session);
                        break;
                    }

                    string messageForCallback = rawMessage;

                    if (IsBroadcast(text))
                    {
                        // Tolgo "!" e il prefisso "[nome]" prima di inoltrare
                        messageForCallback = text[BROADCAST_PREFIX.Max(p => p.Length)..];
                        ForwardMessage(messageForCallback, session);
                    }
                    else if (IsPrivateCommand(text))
                    {
                        HandlePrivateMessage(session, text);
                    }
                    else if (IsSnapshotCommand(text))
                    {
                        HandleSnapshotCommand(session, text);
                    }

                    messageCallback?.Invoke(messageForCallback);
                }
            }
            catch (Exception ex) when (IsSocketClosed(ex))
            {
                // Connessione interrotta o socket chiuso da un altro thread
                Log($"Connessione interrotta con {session.Name}");
            }
            finally
            {
                DropClient(session);
            }
        });
    }

    // Il client antepone "[nome]" al testo: i controlli vanno fatti sul testo senza il prefisso
    private static string StripClientPrefix(string message)
    {
        if (!message.StartsWith('['))
        {
            return message;
        }

        int closeBracketIndex = message.IndexOf(']');
        return closeBracketIndex == -1 ? message : message[(closeBracketIndex + 1)..];
    }

    private static bool IsBroadcast(string text) =>
        BROADCAST_PREFIX.Any(prefix => text.StartsWith(prefix)) && text.Length > BROADCAST_PREFIX.Max(p => p.Length);

    private static bool IsPrivateCommand(string text) =>
        PrivateCommands.Any(cmd => text == cmd || text.StartsWith(cmd + " "));
    private static bool IsSnapshotCommand(string text) =>
        SnapshotCommands.Any(cmd => text == cmd || text.StartsWith(cmd + " "));

    private void HandleExit(ClientSession session)
    {
        if (RemoveSession(session))
        {
            Log($"{session.Name} ha inviato un comando di uscita ed e' uscito dalla sessione");
            TrySendMessageBroadcast($"{session.Name} left from the session");
        }
    }

    // Struttura attesa: /w NomeDestinatario Messaggio
    private void HandlePrivateMessage(ClientSession sender, string text)
    {
        if (!TryParsePrivateMessage(text, out string receiverName, out string body))
        {
            Log($"Messaggio privato con sintassi errata da {sender.Name}");
            TrySend(sender, "ERRORE: Sintassi errata. Usa /w Nome Messaggio");
            return;
        }

        Log($"Messaggio privato da {sender.Name} a {receiverName}: {body}");

        ClientSession target = FindSession(s =>
            string.Equals(s.Name, receiverName, StringComparison.OrdinalIgnoreCase));

        if (target is null)
        {
            Log($"Messaggio privato non consegnato: {receiverName} non esiste");
            TrySend(sender, $"CLIENT {receiverName} NON ESISTE");
        }
        else
        {
            TrySend(target, $"{sender.Name} {body}");
        }
    }

    private void HandleSnapshotCommand(ClientSession sender, string text)
    {
        ClientSession[] snapshot = Snapshot();
        if (snapshot.Length == 0)
        {
            TrySend(sender, "NESSUN CLIENT CONNESSO");
            return;
        }

        StringBuilder sb = new();
        sb.AppendLine("CLIENT CONNESSI:");
        foreach (ClientSession session in snapshot)
        {
            sb.AppendLine($"- {session.Name}");
        }

        TrySend(sender, sb.ToString());
    }

    private static bool TryParsePrivateMessage(string text, out string receiverName, out string body)
    {
        receiverName = body = string.Empty;

        // Salto il comando ("/w ") e cerco lo spazio che separa destinatario e testo
        int nameStart = text.IndexOf(' ') + 1;
        if (nameStart == 0 || nameStart >= text.Length)
        {
            return false;
        }

        int nameEnd = text.IndexOf(' ', nameStart);
        if (nameEnd == -1 || nameEnd == nameStart || nameEnd + 1 >= text.Length)
        {
            return false;
        }

        receiverName = text[nameStart..nameEnd];
        body = text[(nameEnd + 1)..];
        return true;
    }

    // Inoltra a tutti tranne al mittente
    private void ForwardMessage(string message, ClientSession sender)
    {
        foreach (ClientSession session in Snapshot())
        {
            if (session != sender)
            {
                TrySend(session, message);
            }
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
        catch (Exception ex) when (IsSocketClosed(ex))
        {
            DropClient(session);
            return false;
        }
    }

    private ClientSession[] Snapshot()
    {
        lock (sync)
        {
            return sessions.ToArray();
        }
    }

    private ClientSession FindSession(Func<ClientSession, bool> predicate)
    //Func<ClientSession, bool> è un delegato che rappresenta un metodo che prende un ClientSession come input e restituisce un valore booleano.
    //In questo caso, viene utilizzato per filtrare la lista delle sessioni dei client in base a una condizione specifica.
    {
        lock (sync)
        {
            return sessions.FirstOrDefault(predicate);
        }
    }

    // Log interno del server (console), con orario
    private static void Log(string message) =>
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [SERVER] {message}");

    private static bool IsSocketClosed(Exception ex) =>
        ex is SocketException or ObjectDisposedException;

    private static void CloseSocketQuietly(Socket socket)
    {
        try
        {
            socket.Close();
        }
        catch (Exception ex) when (IsSocketClosed(ex))
        {
        }
    }
}