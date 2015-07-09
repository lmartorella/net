using System.Threading;
using System.Windows;

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

        private Semaphore _semaphore;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Get the instance name, accessing the other process names and assigning a progressive number
            InstanceIndex = GetInstanceIndex("Home_WpfMock");
        }

        public int InstanceIndex { get; private set; }

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
