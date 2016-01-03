using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Lucky.Home.Sinks;
using Lucky.Services;

namespace Lucky.Home.Devices
{
    internal abstract class DeviceBase : IDeviceInternal
    {
        private readonly Type[] _requiredSinkTypes;
        private readonly ObservableCollection<SubSink> _sinks = new ObservableCollection<SubSink>();
        private bool _isFullOnline;

        protected DeviceBase()
        {
            var attr = (DeviceAttribute)GetType().GetCustomAttribute(typeof(DeviceAttribute));
            if (attr == null)
            {
                throw new ArgumentNullException("Missing mandatory Device attribute on type " + GetType().FullName);
            }
            _requiredSinkTypes = attr.RequiredSinkTypes;

            _sinks.CollectionChanged += (sender, args) =>
            {
                if ((args.NewItems != null && args.NewItems.Count > 1) || (args.OldItems != null && args.OldItems.Count > 1))
                {
                    throw new InvalidOperationException("Multiple sink change");
                }
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Reset:
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Move:
                        throw new InvalidOperationException("Sink collection operation not supported: " + args.Action);
                    case NotifyCollectionChangedAction.Add:
                        OnSinkChanged(null, (SubSink) args.NewItems[0]);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        OnSinkChanged((SubSink) args.OldItems[0], null);
                        break;
                }
            };
        }

        protected SubSink[] Sinks
        {
            get
            {
                return _sinks.ToArray();
            }
        }

        protected virtual void OnSinkChanged(SubSink removed, SubSink added)
        {
        }

        public SinkPath[] SinkPaths { get; private set; }

        public virtual void OnInitialize(SinkPath[] sinkPaths)
        {
            SinkPaths = sinkPaths;
            var sinkManager = Manager.GetService<SinkManager>();
            lock (sinkManager.LockObject)
            {
                foreach (var sinkPath in SinkPaths)
                {
                    var sink = sinkManager.FindOwnerSink(sinkPath);
                    if (sink != null)
                    {
                        _sinks.Add(new SubSink(sink, sinkPath.SubIndex));
                    }
                }
                sinkManager.CollectionChanged += HandleSinkChanged;
            }
            OnSinkChanged();
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
                _sinks.Clear();
            }
        }

        private void HandleSinkChanged(object sender, CollectionChangeEventArgs e)
        {
            ISink item = (ISink)e.Element;
            if (e.Action == CollectionChangeAction.Add)
            {
                var subSinkPaths = SinkPaths.Where(sp => sp.Equals(item.Path));
                foreach (var sinkPath in subSinkPaths)
                {
                    _sinks.Add(new SubSink(item, sinkPath.SubIndex));
                }
            }
            else if (e.Action == CollectionChangeAction.Remove)
            {
                var sinks = _sinks.Where(s => s.Sink == item);
                foreach (var sink in sinks)
                {
                    _sinks.Remove(sink);
                }
            }
            OnSinkChanged();
        }

        public event EventHandler IsFullOnlineChanged;

        private void OnSinkChanged()
        {
            IsFullOnline = _requiredSinkTypes.All(t => Sinks.Any(s => t.IsInstanceOfType(s.Sink)));
        }

        public bool IsFullOnline
        {
            get
            {
                return _isFullOnline;
            }
            private set
            {
                if (_isFullOnline != value)
                {
                    _isFullOnline = value;
                    if (IsFullOnlineChanged != null)
                    {
                        IsFullOnlineChanged(this, EventArgs.Empty);
                    }
                }
            }
        }
    }
}