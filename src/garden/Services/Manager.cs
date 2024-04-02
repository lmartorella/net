using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lucky.Home.Services
{
    /// <summary>
    /// Service manager
    /// </summary>
    public class Manager
    {
        private readonly ServiceProvider serviceProvider;
        private readonly HostApplicationBuilder hostAppBuilder;
        
        public Manager(string[] args)
        {
            hostAppBuilder = Host.CreateApplicationBuilder(args);
            hostAppBuilder.Services.AddLogging();
            serviceProvider = hostAppBuilder.Services.BuildServiceProvider();

            var wrkPath = hostAppBuilder.Configuration["wrk"];
            if (wrkPath != null) 
            {
                hostAppBuilder.Configuration.SetBasePath(wrkPath);
            }
            var configuration = hostAppBuilder.Configuration.AddJsonFile(@"server.json", optional: true).Build();
            hostAppBuilder.Services.AddScoped<IConfiguration>(_ => configuration);
        }

        public void Start() 
        {
            var host = hostAppBuilder!.Build();
            host.Run();
        }

        public void Register<TC, TI>() where TI : class 
                                              where TC : class, TI
        {
            hostAppBuilder.Services.AddSingleton<TI, TC>();
        }

        public void Register<TC>() where TC : class
        {
            hostAppBuilder.Services.AddSingleton<TC>();
        }

        public void RegisterHostedService<TC>() where TC : class, IHostedService
        {
            hostAppBuilder.Services.AddSingleton<TC>();
            hostAppBuilder.Services.AddHostedService<TC>();
        }
    }
}
