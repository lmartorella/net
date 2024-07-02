using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lucky.Home.Services;

/// <summary>
/// Service manager
/// </summary>
public class Manager
{
    private readonly HostApplicationBuilder hostAppBuilder;

    public Manager(string[] args, string jsonFileName)
    {
        hostAppBuilder = Host.CreateApplicationBuilder(args);
        hostAppBuilder.Services.AddLogging(options =>
        {
            options.AddConsole(c =>
            {
                c.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                c.LogToStandardErrorThreshold = LogLevel.Critical;
            });
        });

        var wrkPath = hostAppBuilder.Configuration["wrk"];
        if (wrkPath != null) 
        {
            Environment.CurrentDirectory = wrkPath;
        }
        hostAppBuilder.Configuration.SetBasePath(Environment.CurrentDirectory);
        
        var configuration = hostAppBuilder.Configuration.AddJsonFile(@"server/" + jsonFileName, optional: false).Build();
        hostAppBuilder.Services.AddScoped<IConfiguration>(_ => configuration);
    }

    public void Start() 
    {
        var host = hostAppBuilder!.Build();
        AppDomain.CurrentDomain.UnhandledException += (o, e) => 
        {
            var logger = host.Services.GetService<ILogger<Manager>>()!;
            if (e.IsTerminating)
            {
                logger.LogCritical(e.ExceptionObject as Exception, "UnhandledException");
            }
            else
            {
                logger.LogError(e.ExceptionObject as Exception, "UnhandledException");
            }
        };
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
        hostAppBuilder.Services.AddHostedService<TC>(services => services.GetService<TC>()!);
    }
}
