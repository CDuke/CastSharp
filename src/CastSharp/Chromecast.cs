using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Policy;
using System.Threading.Tasks;
using CastSharp.Mdns;

namespace CastSharp
{
    public class Chromecast
    {

        public static async Task<List<Chromecast>> GetDevices()
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if ((networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                    || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    && networkInterface.SupportsMulticast
                    && networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    var mullticastClient = new MulticastClient(networkInterface, "_googlecast._tcp.local");
                    var awaitable = new SocketAwaitable.SocketAwaitable();
                    await mullticastClient.SendAsync(awaitable);
                    awaitable.Buffer = new ArraySegment<byte>(new byte[9000]);
                    while (true)
                    {
                        var result = await mullticastClient.ReceiveAsync(awaitable);

                        var stream = new MemoryStream(awaitable.Transferred.Array, awaitable.Transferred.Offset, awaitable.Transferred.Count);
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
                                        //address.ScopeId = ipProperties.GetIPv6Properties().Index;
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
                                        Debug.WriteLine("Receive from: " + serviceName);
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
