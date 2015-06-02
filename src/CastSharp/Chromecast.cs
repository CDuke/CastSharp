using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Policy;
using System.Threading.Tasks;
using CastSharp.Mdns;
using CastSharp.SocketAwaitable;

namespace CastSharp
{
    public class Chromecast
    {
        private static readonly IPEndPoint _multicastEndpoint = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);
        private static readonly Random _randomGenerator = new Random();

        public static async Task<List<Chromecast>> GetDevices()
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if ((networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                    || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    && networkInterface.SupportsMulticast
                    && networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    var ipProperties = networkInterface.GetIPProperties();

                    //var udpClient = new UdpClient();
                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    var index = ipProperties.GetIPv4Properties().Index;
                    socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.HostToNetworkOrder(index));
                    var ip = _multicastEndpoint.Address;
                    socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ip, index));
                    socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 255);
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);
                    socket.Bind(new IPEndPoint(IPAddress.Any, _multicastEndpoint.Port));

                    var lastQueryId = (ushort)_randomGenerator.Next(0, ushort.MaxValue);
                    var writer = new DnsMessageWriter();
                    writer.WriteQueryHeader(lastQueryId);
                    writer.WriteQuestion(new Name("_googlecast._tcp.local"), RecordType.PTR);

                    var packet = writer.Packets[0];
                    var awaitable = new SocketAwaitable.SocketAwaitable();
                    awaitable.Buffer = packet;
                    awaitable.Arguments.RemoteEndPoint = _multicastEndpoint;
                    await socket.SendToAsync(awaitable);


                    while (true)
                    {
                        var result = await socket.ReceiveAsync(awaitable);

                        var stream = new MemoryStream(awaitable.Transferred.Array, 0, awaitable.Transferred.Array.Length);
                        var reader = new DnsMessageReader(stream);
                        var header = reader.ReadHeader();
                        if (header.IsResponse && header.IsNoError && header.IsAuthorativeAnswer)
                        {
                            for (var i = 0; i < header.QuestionCount; i++)
                            {
                                reader.ReadQuestion();
                            }
                            for (var i = 0; i < (header.AnswerCount + header.AuthorityCount + header.AdditionalCount); i++)
                            {
                                var recordHeader = reader.ReadRecordHeader();
                                if ((recordHeader.Type == RecordType.A) || (recordHeader.Type == RecordType.AAAA)) // A or AAAA
                                {
                                    var address = reader.ReadARecord();
                                    if (address.AddressFamily == AddressFamily.InterNetworkV6)
                                    {
                                        if (!networkInterface.Supports(NetworkInterfaceComponent.IPv6))
                                        {
                                            continue;
                                        }
                                        address.ScopeId = ipProperties.GetIPv6Properties().Index;
                                    }
                                    //OnARecord(recordHeader.Name, address, recordHeader.Ttl);
                                }
                                else if ((recordHeader.Type == RecordType.SRV)
                                    || (recordHeader.Type == RecordType.TXT)
                                    || (recordHeader.Type == RecordType.PTR))
                                {
                                    Name serviceName;
                                    Name instanceName;
                                    if (recordHeader.Type == RecordType.PTR)
                                    {
                                        serviceName = recordHeader.Name;
                                        instanceName = reader.ReadPtrRecord();
                                    }
                                    else
                                    {
                                        instanceName = recordHeader.Name;
                                        serviceName = instanceName.SubName(1);
                                    }
                                    if (recordHeader.Ttl == 0)
                                    {
                                        //PacketRemovesService(instanceName);
                                    }
                                    else
                                    {
                                        //ServiceInfo service = FindOrCreatePacketService(instanceName);
                                        if (recordHeader.Type == RecordType.SRV)
                                        {
                                            SrvRecord srvRecord = reader.ReadSrvRecord();
                                            //service.HostName = srvRecord.Target;
                                            //service.Port = srvRecord.Port;
                                        }
                                        else if (recordHeader.Type == RecordType.TXT)
                                        {
                                            var txts = reader.ReadTxtRecord();
                                            //service.Txt = txts;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        public async Task Connect()
        {
            throw new NotImplementedException();
        }

        public async Task Disconnect()
        {
            throw new NotImplementedException();
        }

        public async Task LaunchApplication(string applicationId)
        {
            throw new NotImplementedException();
        }

        public async Task Load(Url url)
        {
            throw new NotImplementedException();
        }
    }
}
