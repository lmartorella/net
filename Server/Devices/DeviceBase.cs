using System.ComponentModel;
using Lucky.Home.Sinks;
using Lucky.Services;

namespace Lucky.Home.Devices
{
    internal abstract class DeviceBase<T> : IDeviceIntenal where T : class, ISink
    {
        public T Sink { get; private set; }

        public SinkPath SinkPath { get; private set; }

        public void OnInitialize(SinkPath sinkPath)
        {
            SinkPath = sinkPath;
            var sinkManager = Manager.GetService<SinkManager>();
            lock (sinkManager.LockObject)
            {
                Sink = (T)sinkManager.FindSink(SinkPath);
                sinkManager.CollectionChanged += HandleSinkChanged;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            var sinkManager = Manager.GetService<SinkManager>();
            lock (sinkManager.LockObject)
            {
                sinkManager.CollectionChanged -= HandleSinkChanged;
                Sink = null;
            }
        }

        private void HandleSinkChanged(object sender, CollectionChangeEventArgs e)
        {
            ISink item = (ISink)e.Element;
            if (Sink == null && e.Action == CollectionChangeAction.Add)
            {
                if (item.Path.Equals(SinkPath))
                {
                    Sink = (T)item;
                }
            }
            else if (Sink == item && e.Action == CollectionChangeAction.Remove)
            {
                Sink = null;
            }
        }

        public bool IsOnline
        {
            get
            {
                return Sink != null;
            }
        }
    }
}