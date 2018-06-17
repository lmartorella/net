using Lucky.Services;
using Lucky.Home.Devices;
using Lucky.Home.Sinks;
using System.Reflection;
using Lucky.Home.Application;
using Lucky.Home.Services;
using System.Threading.Tasks;

namespace Lucky.Home.Lib
{
    static class Program
    {
        public static void Main(string[] arguments)
        {
            ServerEntryPoint.Load(() =>
            {
                // Register app sinks
                Manager.GetService<ISinkManager>().RegisterAssembly(Assembly.GetExecutingAssembly());
                // Register devices
                Manager.GetService<IDeviceManager>().RegisterAssembly(Assembly.GetExecutingAssembly());
            }, arguments);


            _ = Manager.GetService<HomeApp>().Start();

            Manager.GetService<PipeServer>().Message += (o, e) =>
            {
                if (e.Request.Command == "kill")
                {
                    e.CloseServer = true;
                    e.Response.Status= "Closed";

                    Task.Delay(1500).ContinueWith(t =>
                    {
                        Manager.GetService<HomeApp>().Kill("killed by parent process");
                    });
                }
            };

            Manager.GetService<HomeApp>().Run();
        }
    }
}
