using Lucky.Home.Sinks;
using Lucky.Home.Services;
using Lucky.Home.Protocol;
using Lucky.Home.Admin;
using System.Threading.Tasks;

namespace Lucky.Home
{
    static class Program
    {
        public static async Task Main(string[] arguments)
        {
            var logger = await Bootstrap.Start(arguments, "server");

            Manager.Register<Server>();
            Manager.Register<NodeManager>();
            Manager.Register<SinkManager, ISinkManager>();

            // Start server
            Manager.GetService<Server>();

            // Start Admin connection
            Manager.GetService<AdminListener>();

            Samples.Load();

            await Manager.Run();

            // Safely stop devices
            logger.Log("Exiting.");
        }
    }
}
