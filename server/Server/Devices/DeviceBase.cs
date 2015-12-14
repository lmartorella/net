using System.ComponentModel;
using Lucky.Home.Sinks;
using Lucky.Services;

namespace Lucky.Home.Devices
{
    internal abstract class DeviceBase<T> : IDeviceInternal where T : class, ISink
    {
        private T _sink;

        protected T Sink
        {
            get
            {
                return _sink;
            }
            private set
            {
                if (_sink != value)
                {
                    _sink = value;
                    OnSinkChanged();
                }
            }
        }

        protected virtual void OnSinkChanged()
        {
        }

        public SinkPath SinkPath { get; private set; }

        public string Argument { get; private set; }

        public virtual void OnInitialize(string argument, SinkPath sinkPath)
        {
            Argument = argument;
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