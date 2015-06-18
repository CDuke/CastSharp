using System.Net;

namespace CastSharp
{
    public class ChromecastDeviceInfo
    {
        public string Name { get; set; }

        public string Id { get; set; }

        public IPEndPoint EndPoint { get; set; }

        public int Version { get; set; }
    }
}
