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

                    onMessageReceived(decodedMessage);
                }
            });
        }
    }
}