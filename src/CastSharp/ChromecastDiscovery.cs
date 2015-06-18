using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using CastSharp.Mdns;

namespace CastSharp
{
    public class ChromecastDiscovery
    {
        private const string chromecastPtr = "_googlecast._tcp.local.";

        public async Task<ChromecastDeviceInfo> GetDevice()
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if ((networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                    || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    && networkInterface.SupportsMulticast
                    && networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    var mullticastClient = new MulticastClient(networkInterface, chromecastPtr);
                    var awaitable = new SocketAwaitable.SocketAwaitable();
                    await mullticastClient.SendAsync(awaitable);
                    awaitable.Buffer = new ArraySegment<byte>(new byte[9000]);
                    while (true)
                    {
                        var result = await mullticastClient.ReceiveAsync(awaitable);

                        var stream = new MemoryStream(awaitable.Transferred.Array, awaitable.Transferred.Offset, awaitable.Transferred.Count);
                        var reader = new DnsMessageReader(stream);
                        var chromecastDeviceInfo = TryReadChromecastDeviceInfo(reader);
                        if (chromecastDeviceInfo != null)
                            return chromecastDeviceInfo;
                    }
                }
            }

            return null;
        }

        private static ChromecastDeviceInfo TryReadChromecastDeviceInfo(DnsMessageReader reader)
        {
            ChromecastDeviceInfo chromecastDeviceInfo = null;

            var header = reader.ReadHeader();
            if (header.IsResponse && header.IsNoError && header.IsAuthorativeAnswer)
            {
                chromecastDeviceInfo = new ChromecastDeviceInfo();
                var isChromecastDevice = false;
                for (var i = 0; i < header.QuestionCount; i++)
                {
                    reader.ReadQuestion();
                }
                IPAddress ipAddress = null;
                var port = 0;
                for (var i = 0; i < (header.AnswerCount + header.AuthorityCount + header.AdditionalCount); i++)
                {
                    var recordHeader = reader.ReadRecordHeader();
                    Name instanceName;
                    Name serviceName = null;
                    switch (recordHeader.Type)
                    {
                        case RecordType.A:
                        case RecordType.AAAA:
                            serviceName = new Name(chromecastPtr);
                            ipAddress = reader.ReadARecord();
                            break;
                        case RecordType.PTR:
                            instanceName = reader.ReadPtrRecord();
                            serviceName = recordHeader.Name;
                            break;
                        case RecordType.SRV:
                            instanceName = recordHeader.Name;
                            serviceName = instanceName.SubName(1);
                            var srvRecord = reader.ReadSrvRecord();
                            port = srvRecord.Port;
                            break;
                        case RecordType.TXT:
                            instanceName = recordHeader.Name;
                            serviceName = instanceName.SubName(1);
                            var txts = reader.ReadTxtRecord();
                            ParseTxtRecord(txts, chromecastDeviceInfo);
                            break;
                    }

                    if (!IsChromecastDevice(serviceName))
                    {
                        chromecastDeviceInfo = null;
                        break;
                    }
                }

                if (chromecastDeviceInfo != null)
                {
                    chromecastDeviceInfo.EndPoint = new IPEndPoint(ipAddress, port);
                }
            }

            return chromecastDeviceInfo;
        }

        private static void ParseTxtRecord(List<string> txtRecord,
            ChromecastDeviceInfo chromecastDeviceInfo)
        {
            foreach (var txt in txtRecord)
            {
                var keyValue = txt.Split('=');
                var key = keyValue[0];
                if (key == "id")
                {
                    chromecastDeviceInfo.Id = keyValue[1];
                }
                else if (key == "ve")
                {
                    chromecastDeviceInfo.Version = Convert.ToInt32(keyValue[1]);
                }
                else if (key == "fn")
                {
                    chromecastDeviceInfo.Name = keyValue[1];
                }
            }
        }

        private static bool IsChromecastDevice(Name serviceName)
        {
            return serviceName == null || serviceName.ToString() == chromecastPtr;
        }
    }
}
