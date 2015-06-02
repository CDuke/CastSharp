using System.Threading.Tasks;
using Xunit;

namespace CastSharp.Tests
{
    public class Test
    {
        [Fact]
        public async Task FactMethodName()
        {
            var a = await Chromecast.GetDevices();
        }
    }
}
