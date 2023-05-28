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

            var flowSink = new FlowRpc();
            var gardenSink = new GardenRpc();
            var pumpSink = new DigitalInputArrayRpc();
            var device = new GardenDevice(gardenSink, flowSink, pumpSink);
            Manager.Killed += (o, e) => device.OnTerminate();
            await device.StartLoop();
        }
    }
}
