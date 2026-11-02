using ProtocolSystem;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Core
{
    public class Client
    {
        private const string DEFAULT_HOSTNAME_NAME = "localhost";
        private const int BUFFER_SIZE = 1024;
        private int MAX_CONNECTION_ATTEMPTS = 10;

        private Socket communicationChannel;

        public int Port { get; init; }
        public string HostName { get; init; }
        public string UserName { get; init; } //SE LO USERNAME E' NULL A NOI CLIENT NON INTERESSA, CIO' SERVE SOLO AL SERVER

        public bool IsConnected => communicationChannel is not null && communicationChannel.Connected;

        private byte[] buffer;

        public Client(ClientConfiguration configuration)
        {
            this.Port = configuration.Port;
            this.HostName = configuration.HostName;
            this.UserName = configuration.UserName;
            ClearBuffer();
        }

        private void ClearBuffer()
        {
            buffer = new byte[BUFFER_SIZE];
        }

        private IPEndPoint ResolveEndpoint()
        {
            var hostIP = Dns
            .GetHostEntry(HostName)
            .AddressList
            .First();
            var remoteEndpoint = new IPEndPoint(hostIP, Port);
            return remoteEndpoint;
        }

        private Socket? TryConnectOnce(IPEndPoint remoteEndpoint)
        {
            var hostIP = Dns
            .GetHostEntry(HostName)
            .AddressList
            .First();

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
                return null;
            }
            return socket;
        }

        // Connette e, se riesce, manda subito l'handshake con il nome utente:
        // l'app non puo' dimenticarsene.
        public void Connect()
        {
            var remoteEndpoint = ResolveEndpoint();

            for (int i = 0; i < MAX_CONNECTION_ATTEMPTS; i++)
            {
                var result = TryConnectOnce(remoteEndpoint);
                if (result is not null)
                {
                    this.communicationChannel = result;
                    break;
                }
                Thread.Sleep(500);
            }

            if (IsConnected)
            {
                TrySend(Message.Handshake(UserName));
            }
        }

        public bool TrySendText(string text, string? recipient = null)
        {
            return TrySend(Message.Text(text, recipient));
        }

        public void Disconnect()
        {
            if (communicationChannel is null)
            {
                return;
            }

            if (IsConnected)
            {
                TrySend(Message.Disconnect());
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

        private bool TrySend(Message message)
        {
            if (!IsConnected)
            {
                return false;
            }

            string text;
            try
            {
                // Format valida il messaggio e lancia ArgumentException se non e' valido
                text = MessageFormatter.Format(message);
            }
            catch (ArgumentException)
            {
                return false;
            }

            byte[] raw = Encoding.UTF8.GetBytes(text);

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

        // Da byte a testo (UTF-8), da testo a Message (TryParse), poi la callback.
        // I messaggi che non rispettano il protocollo vengono scartati.
        public void StartListening(Action<Message> onMessageReceived)
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
                        return;
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }

                    if (receivedBytes == 0)
                    {
                        return;
                    }

                    string decodedMessage = Encoding.UTF8.GetString(buffer, 0, receivedBytes);

                    if (!MessageFormatter.TryParse(decodedMessage, out Message? message, out _))
                    {
                        continue;
                    }

                    onMessageReceived(message);
                }
            });
        }
    }
}