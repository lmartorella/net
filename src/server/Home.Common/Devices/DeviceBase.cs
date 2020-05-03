using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Lucky.Home.Sinks;
using Lucky.Home.Services;

namespace Lucky.Home.Devices
{
    /// <summary>
    /// Base class for device objects
    /// </summary>
    public abstract class DeviceBase : IDevice
    {
        private readonly RequiresAttribute[] _requiredSinkTypes;
        private readonly ObservableCollection<SubSink> _sinks = new ObservableCollection<SubSink>();
        private OnlineStatus _onlineStatus;
        protected readonly ILogger Logger;

        protected DeviceBase()
        {
            Logger = Manager.GetService<LoggerFactory>().Create(GetType().Name);
            _requiredSinkTypes = (RequiresAttribute[])GetType().GetCustomAttributes(typeof(RequiresAttribute));
            if (_requiredSinkTypes == null || _requiredSinkTypes.Length == 0)
            {
                throw new ArgumentNullException("Missing mandatory Requires/RequiresArray attribute on type " + GetType().FullName);
            }

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

        /// <summary>
        /// Get the list of the attached sinks
        /// </summary>
        protected ISink[] Sinks
        {
            get
            {
                return _sinks.Select(s => s.Sink).ToArray();
            }
        }

        /// <summary>
        /// Called when the list of attached sinks changes
        /// </summary>
        protected virtual void OnSinkChanged(SubSink removed, SubSink added)
        {
        }

        /// <summary>
        /// Get the serializable list of sinks (by path)
        /// </summary>
        internal SinkPath[] SinkPaths { get; private set; }

        internal void OnInitialize(SinkPath[] sinkPaths)
        {
            SinkPaths = sinkPaths;
            var sinkManager = Manager.GetService<ISinkManager>();
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
            OnInitialize();
        }

        protected virtual void OnInitialize()
        {

        }

        protected internal virtual Task OnTerminate()
        {
            var sinkManager = Manager.GetService<ISinkManager>();
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
            return Task.CompletedTask;
        }

        private void HandleSinkChanged(object sender, CollectionChangeEventArgs e)
        {
            SinkBase item = (SinkBase)e.Element;
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

        /// <summary>
        /// Raised when all the registered sinks are registered
        /// </summary>
        public event EventHandler OnlineStatusChanged;

        private void OnSinkChanged()
        {
            if (_requiredSinkTypes.All(t => Sinks.Any(s => t.Type.IsInstanceOfType(s) && s.IsOnline)))
            {
                // Full online
                OnlineStatus = OnlineStatus.Online;
            }
            else if (_requiredSinkTypes.Where(t => !t.Optional).All(t => Sinks.Any(s => t.Type.IsInstanceOfType(s) && s.IsOnline)))
            {
                OnlineStatus = OnlineStatus.PartiallyOnline;
            }
            else
            {
                OnlineStatus = OnlineStatus.Offline;
            }
        }

        protected T GetFirstOnlineSink<T>() where T : SinkBase
        {
            return Sinks.OfType<T>().FirstOrDefault(s => s.IsOnline);
        }

        public OnlineStatus OnlineStatus
        {
            get
            {
                return _onlineStatus;
            }
            private set
            {
                if (_onlineStatus != value)
                {
                    _onlineStatus = value;
                    OnlineStatusChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        protected bool IsDisposed { get; private set; }
    }
}