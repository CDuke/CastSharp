using System;
using System.Security.Policy;
using System.Threading.Tasks;

namespace CastSharp
{
    public class Chromecast
    {
        private static readonly ChromecastDiscovery _chromecastDiscovery = new ChromecastDiscovery();

        private readonly ChromecastDeviceInfo _chromecastDeviceInfo;

        internal Chromecast(ChromecastDeviceInfo chromecastDeviceInfo)
        {
            _chromecastDeviceInfo = chromecastDeviceInfo;
        }

        public static async Task<Chromecast> GetDevice()
        {
            var chromecastDeviceInfo = await _chromecastDiscovery.GetDevice();
            return new Chromecast(chromecastDeviceInfo);
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
