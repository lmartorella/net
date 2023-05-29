using Lucky.Home;
using Lucky.Home.Devices.Garden;
using Lucky.Home.Services;
using System.Threading.Tasks;

namespace Home.Garden
{
    class Program
    {
        public static async Task Main(string[] arguments)
        {
            await Bootstrap.Start(arguments, "garden");

            var device = new GardenDevice(new GardenRpc(), new FlowRpc(), new DigitalInputArrayRpc());
            Manager.Killed += (o, e) => device.OnTerminate();
            await device.StartLoop();
        }
    }
}
