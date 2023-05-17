using Lucky.Home.Services;
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
            _ = Bootstrap.Start(e.Args, "simulator.ui");

            base.OnStartup(e);

            Manager.GetService<MockSinkManager>();
        }
    }
}