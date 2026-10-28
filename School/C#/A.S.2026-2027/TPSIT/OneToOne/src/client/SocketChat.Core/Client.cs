namespace SocketChat.Core;

using System.Net;
using System.Net.Sockets;
using System.Text;

public class Client
{
    private const string DEFAULT_HOST_NAME = "localhost";
    private const string IDENTITY_PREFIX = "assign_identity: ";
    private const int MAX_RECONNECTION_ATTEMPTS = 10;
    private readonly int BUFFER_SIZE = 1024;
    private Socket communicationChannel;
    public int Port { get; init; }
    public string HostName { get; init; }

    public string AssignedName {get; private set;}
    public bool IsConnected => communicationChannel is not null && communicationChannel.Connected;
    private byte[] buffer;
    
    public Client(int port, string hostName)
    {
        this.Port = port;
        this.HostName = hostName ?? DEFAULT_HOST_NAME;
        ClearBuffer();
    }

    private void ClearBuffer()
    {
        this.buffer = new byte[BUFFER_SIZE];
    }

    public void Connect()
    {
        var hostIP = Dns
            .GetHostEntry(HostName)
            .AddressList
            .First();
        var remoteEndpoint = new IPEndPoint(hostIP, Port);

        for (int attempts = 0; attempts < MAX_RECONNECTION_ATTEMPTS && !IsConnected; attempts++)
        {
            // Un socket dopo un Connect fallito non e' affidabile: ne creo uno nuovo a ogni tentativo
            var socket = new Socket(
                hostIP.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp
            );

            try
            {
                socket.Connect(remoteEndpoint);
                this.communicationChannel = socket;
            }
            catch (Exception)
            {
                socket.Dispose();
                Thread.Sleep(500);
            }
        }
    }

    public bool TrySendMessage(string msg)
    {
        if (!IsConnected)
        {
            return false;
        }
        msg = $"[{AssignedName}]" + msg;

        byte[] raw = Encoding.UTF8.GetBytes(msg);

        try
        {
            int sent = communicationChannel.Send(raw);
            return sent == raw.Length;
        }
        catch (SocketException)
        {
            return false;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    public void StartListening(Action<string> onMessageReceived)
    {
        Task.Run(() =>
        {
            ClearBuffer();
            while (IsConnected)
            {
                int receivedBytes;
                try
                {
                    receivedBytes = this.communicationChannel.Receive(this.buffer);
                }
                catch (SocketException)
                {
                    // Connessione interrotta
                    return;
                }
                catch (ObjectDisposedException)
                {
                    // Socket chiuso localmente
                    return;
                }

                if (receivedBytes == 0)
                {
                    // Server just disconnected
                    return;
                }
                string decodedMessage = Encoding.UTF8.GetString(buffer, 0, receivedBytes);

                // Il nome si assegna solo se il messaggio e' davvero quello di identita'
                if (AssignedName is null && decodedMessage.StartsWith(IDENTITY_PREFIX))
                {
                    AssignName(decodedMessage.Substring(IDENTITY_PREFIX.Length));
                }
                onMessageReceived(decodedMessage);
            }
        });
    }

    public void AssignName(string name)
    {
        AssignedName = name;
    }

    public void Close()
    {
        if (communicationChannel is null)
        {
            return;
        }

        try
        {
            communicationChannel.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException)
        {
            // Socket gia' disconnesso
        }
        catch (ObjectDisposedException)
        {
        }

        communicationChannel.Close();
    }
}