using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using CastSharp.SocketAwaitable;

namespace CastSharp.Mdns
{
    internal sealed class MulticastClient
    {
        private readonly string _ptr;
        private static readonly IPEndPoint _multicastEndpoint = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);
        private static readonly Random _randomGenerator = new Random();
        private readonly Socket _socket;
        private readonly SocketAwaitable.SocketAwaitable _socketReceiveAwaitable = new SocketAwaitable.SocketAwaitable();

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

            _socketReceiveAwaitable.Buffer = new ArraySegment<byte>(new byte[9000]);
        }

        public SocketAwaitable.SocketAwaitable SendAsync(SocketAwaitable.SocketAwaitable awaitable)
        {
            var lastQueryId = (ushort)_randomGenerator.Next(0, ushort.MaxValue);
            var writer = new DnsMessageWriter();
            writer.WriteQueryHeader(lastQueryId);
            writer.WriteQuestion(new Name(_ptr), RecordType.PTR);

            var packet = writer.Packets[0];

            awaitable.Buffer = packet;
            awaitable.Arguments.RemoteEndPoint = _multicastEndpoint;
            _socketReceiveAwaitable.RemoteEndPoint = _multicastEndpoint;
            return _socket.SendToAsync(awaitable);
        }

        public SocketAwaitable.SocketAwaitable ReceiveAsync(SocketAwaitable.SocketAwaitable awaitable)
        {
            return _socket.ReceiveAsync(awaitable);
        }

        public void Close()
        {
            _socket.Close();
        }
    }
}
