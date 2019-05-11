using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Lucky.Home.Devices;
using Lucky.Home.Protocol;
using Lucky.Home.Services;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Sink instance manager
    /// </summary>
    class SinkManager : ServiceBase, ISinkManager
    {
        private readonly ObservableCollection<SinkBase> _sinks = new ObservableCollection<SinkBase>();
        public object LockObject { get; private set; }

        public SinkManager()
        {
            LockObject = new object();
        }

        public IEnumerable<T> SinksOfType<T>() where T : ISink
        {
            lock (LockObject)
            {
                return _sinks.OfType<T>().ToArray();
            }
        }

        public SinkBase CreateSink(string sinkFourCc, ITcpNode node, int index)
        {
            Type type = Manager.GetService<SinkTypeManager>().FindType(sinkFourCc);
            if (type == null)
            {
                // Unknown sink type
                Logger.Warning("Unknown sink code", "code", sinkFourCc, "guid", node.NodeId);
                return new SinkBase(sinkFourCc);
            }

            var sink = (SinkBase)Activator.CreateInstance(type);
            sink.Init(node, index);

            lock (LockObject)
            {
                // Communicate new sink to API
                _sinks.Add(sink);
                CollectionChanged?.Invoke(this, new CollectionChangeEventArgs(CollectionChangeAction.Add, sink));
            }

            return sink;
        }

        public void DestroySink(SinkBase sink)
        {
            lock (LockObject)
            {
                // Communicate new sink to API
                if (_sinks.Remove(sink))
                {
                    CollectionChanged?.Invoke(this, new CollectionChangeEventArgs(CollectionChangeAction.Remove, sink));
                }
                else
                {
                    throw new InvalidOperationException("Unexpected sink remove");
                }
            }
        }

        internal void UpdateSinks()
        {
            lock (LockObject)
            {
                CollectionChanged?.Invoke(this, new CollectionChangeEventArgs(CollectionChangeAction.Refresh, null));
            }
        }

        public ISink FindOwnerSink(SinkPath sinkPath)
        {
            // Find the parent sink instance
            lock (LockObject)
            {
                return _sinks.FirstOrDefault(s => s.Path.Owns(sinkPath));
            }
        }

        /// <summary>
        /// Raised if a sink is added, removed or it changes state (e.g. zombie)
        /// </summary>
        public event EventHandler<CollectionChangeEventArgs> CollectionChanged;

        internal class ResetSinkEventArgs : EventArgs
        {
            public readonly SinkBase Sink;

            public ResetSinkEventArgs(SinkBase sink)
            {
                this.Sink = sink;
            }
        }

        internal event EventHandler<ResetSinkEventArgs> ResetSink;

        public void RaiseResetSink(SinkBase sink)
        {
            ResetSink?.Invoke(this, new ResetSinkEventArgs(sink));
        }
    }
}
