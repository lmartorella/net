using System.ComponentModel;
using Lucky.Home.Sinks;
using Lucky.Services;

namespace Lucky.Home.Devices
{
    internal abstract class DeviceBase : IDevice
    {
        private ISink _sink;

        public SinkPath SinkPath { get; private set; }

        internal void OnInitialize(SinkPath sinkPath)
        {
            SinkPath = sinkPath;
            var sinkManager = Manager.GetService<SinkManager>();
            lock (sinkManager.LockObject)
            {
                _sink = sinkManager.FindSink(SinkPath);
                sinkManager.CollectionChanged += HandleSinkChanged;
            }
        }

        public void Dispose()
        {
            var sinkManager = Manager.GetService<SinkManager>();
            lock (sinkManager.LockObject)
            {
                sinkManager.CollectionChanged -= HandleSinkChanged;
                _sink = null;
            }
        }

        private void HandleSinkChanged(object sender, CollectionChangeEventArgs e)
        {
            ISink item = (ISink)e.Element;
            if (_sink == null && e.Action == CollectionChangeAction.Add)
            {
                if (item.Path.Equals(SinkPath))
                {
                    _sink = item;
                }
            }
            else if (_sink == item && e.Action == CollectionChangeAction.Remove)
            {
                _sink = null;
            }
        }

        public bool IsOnline
        {
            get
            {
                return _sink != null;
            }
        }
    }
}