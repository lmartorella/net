using System.Threading;
using System.Windows;
using Lucky.Home.Core;

namespace Lucky.HomeMock
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public static new App Current
        {
            get
            {
                return (App)Application.Current;
            }
        }

        // ReSharper disable once NotAccessedField.Local
        private Semaphore _semaphore;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Get the instance name, accessing the other process names and assigning a progressive number
            _instanceIndex = GetInstanceIndex("Home_WpfMock");

            Manager.Register<GuiLoggerFactory, ILoggerFactory>();
            Manager.GetService<ILoggerFactory>().Create("App").Log("Started", "instance", _instanceIndex);
        }

        private int _instanceIndex;

        private int GetInstanceIndex(string prefix)
        {
            int index = 0;
            Semaphore result;
            while (Semaphore.TryOpenExisting(@"Global\" + prefix + index, out result))
            {
                index++;
            }
            // When in error, it is free
            _semaphore = new Semaphore(0, 1, @"Global\" + prefix + index);
            return index;
        }
    }
}
