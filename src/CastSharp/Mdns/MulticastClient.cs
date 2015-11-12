using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CastSharp.Mdns
{
    internal sealed class MulticastClient : IDisposable
    {
        private readonly string _ptr;
        private static readonly IPEndPoint _multicastEndpoint = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);
        private static readonly Random _randomGenerator = new Random();
        private readonly Socket _socket;

        public MulticastClient(NetworkInterface networkInterface, string ptr)
        {
            _ptr = ptr;

            var ipProperties = networkInterface.GetIPProperties();

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            var index = ipProperties.GetIPv4Properties().Index;
            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.HostToNetworkOrder(index));
            var ip = _multicastEndpoint.Address;
            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ip, index));
            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 100);
            _socket.Bind(new IPEndPoint(IPAddress.Any, _multicastEndpoint.Port));
        }

        public Task<int> SendAsync()
        {
            var lastQueryId = (ushort)_randomGenerator.Next(0, ushort.MaxValue);
            var writer = new DnsMessageWriter();
            writer.WriteQueryHeader(lastQueryId);
            writer.WriteQuestion(new Name(_ptr), RecordType.PTR);

            var packet = writer.Packets[0];

            var udpClient = new UdpClient
            {
                Client = _socket
            };
            return udpClient.SendAsync(packet.Array, packet.Count, _multicastEndpoint);
        }

        public async Task<byte[]> ReceiveAsync()
        {
            var udpClient = new UdpClient
            {
                Client = _socket,
            };
            var res = await udpClient.ReceiveAsync();
            return res.Buffer;
        }

        public void Close()
        {
            _socket.Close();
        }

        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}
