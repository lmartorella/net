using Lucky.Home.Services;
using System.Threading;
using System.Windows;

namespace Lucky.Home
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");

            base.OnStartup(e);

            Manager.Register<JsonIsolatedStorageService, IIsolatedStorageService>();
            Manager.Register<GuiConfigurationService, IConfigurationService>();
            Manager.Register<GuiLoggerFactory, ILoggerFactory>();

            Manager.GetService<IIsolatedStorageService>().InitAppRoot("Manager.UI");
            Manager.GetService<ILoggerFactory>().Create("App").Log("Started");

            Manager.GetService<MockSinkManager>();
            Manager.GetService<Registrar>().LoadLibraries(new[] { typeof(ApplicationAttribute), typeof(UiLibraryAttribute) });
        }

        private class GuiConfigurationService : IConfigurationService
        {
            public void Dispose()
            {
            }

            public string GetConfig(string key)
            {
                return null;
            }
        }
    }
}