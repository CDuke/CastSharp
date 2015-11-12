using System.Net;

namespace CastSharp
{
    public class ChromecastDeviceInfo
    {
        public string Name { get; internal set; }

        public string Id { get; internal set; }

        public IPEndPoint EndPoint { get; internal set; }

        public int Version { get; internal set; }
    }
}
