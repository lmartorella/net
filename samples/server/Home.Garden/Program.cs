using Lucky.Home.Devices.Garden;
using System.Threading.Tasks;

namespace Home.Garden
{
    class Program
    {
        static void Main()
        {
            _ = Start();
        }

        private static async Task Start()
        {
            var flowSink = new FlowRpc();
            var gardenSink = new GardenRpc();
            var pumpSink = new DigitalInputArrayRpc();
            var device = new GardenDevice(gardenSink, flowSink, pumpSink);
            await device.StartLoop();
        }
    }
}
