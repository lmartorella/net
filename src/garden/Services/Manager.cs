using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lucky.Garden.Services
{
    /// <summary>
    /// Service manager
    /// </summary>
    public class Manager
    {
        private readonly HostApplicationBuilder hostAppBuilder;
        
        public Manager(string[] args)
        {
            hostAppBuilder = Host.CreateApplicationBuilder(args);
            hostAppBuilder.Services.AddLogging();

            var wrkPath = hostAppBuilder.Configuration["wrk"];
            if (wrkPath != null) 
            {
                Environment.CurrentDirectory = wrkPath;
            }
            hostAppBuilder.Configuration.SetBasePath(Environment.CurrentDirectory);
            
            var configuration = hostAppBuilder.Configuration.AddJsonFile(@"server/gardenConfiguration.json", optional: false).Build();
            hostAppBuilder.Services.AddScoped<IConfiguration>(_ => configuration);
        }

        public void Start() 
        {
            var host = hostAppBuilder!.Build();
            host.Run();
        }

        public void AddSingleton<TC, TI>() where TI : class 
                                              where TC : class, TI
        {
            hostAppBuilder.Services.AddSingleton<TI, TC>();
        }

        public void AddSingleton<TC>() where TC : class
        {
            hostAppBuilder.Services.AddSingleton<TC>();
        }

        public void AddHostedService<TC>() where TC : class, IHostedService
        {
            hostAppBuilder.Services.AddSingleton<TC>();
            hostAppBuilder.Services.AddHostedService<TC>();
        }
    }
}
