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
    public abstract class DeviceBase : IDeviceInternal
    {
        private readonly Type[] _requiredSinkTypes;
        private readonly ObservableCollection<SubSink> _sinks = new ObservableCollection<SubSink>();
        private bool _isFullOnline;
        protected ILogger Logger;

        protected DeviceBase()
        {
            Logger = Manager.GetService<LoggerFactory>().Create(GetType().Name);
            var attr = (RequiresAttribute[])GetType().GetCustomAttributes(typeof(RequiresAttribute));
            if (attr == null || attr.Length == 0)
            {
                throw new ArgumentNullException("Missing mandatory Requires/RequiresArray attribute on type " + GetType().FullName);
            }
            _requiredSinkTypes = attr.Select(a => a.Type).ToArray();

            _sinks.CollectionChanged += (sender, args) =>
            {
                if ((args.NewItems != null && args.NewItems.Count > 1) || (args.OldItems != null && args.OldItems.Count > 1))
                {
                    throw new InvalidOperationException("Multiple sink change");
                }
                switch (args.Action)
                {
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

        protected ISink[] Sinks
        {
            get
            {
                return _sinks.Select(s => s.Sink).ToArray();
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
                //_sinks.Clear();
                foreach (var sink in _sinks.ToArray())
                {
                    _sinks.Remove(sink);
                }
            }
            IsDisposed = true;
        }

        private void HandleSinkChanged(object sender, CollectionChangeEventArgs e)
        {
            ISink item = (ISink)e.Element;
            if (e.Action == CollectionChangeAction.Add)
            {
                var subSinkPaths = SinkPaths.Where(sp => item.Path.Owns(sp));
                foreach (var sinkPath in subSinkPaths)
                {
                    _sinks.Add(new SubSink(item, sinkPath.SubIndex));
                }
            }
            else if (e.Action == CollectionChangeAction.Remove)
            {
                var sinks = _sinks.Where(s => s.Sink == item).ToArray();
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
            IsFullOnline = _requiredSinkTypes.All(t => Sinks.Any(s => t.IsInstanceOfType(s) && s.IsOnline));
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

        protected bool IsDisposed { get; private set; }
    }
}