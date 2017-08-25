using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Lucky.Home.Devices;
using Lucky.Home.Protocol;
using Lucky.Services;

namespace Lucky.Home.Sinks
{
    public interface ISinkManager : IService
    {
        void RegisterAssembly(Assembly assembly);
        void RegisterType(Type type);
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    class SinkManager : ServiceBase, ISinkManager
    {
        private readonly Dictionary<string, Type> _sinkTypes = new Dictionary<string, Type>();
        private readonly ObservableCollection<ISink> _sinks = new ObservableCollection<ISink>();
        public object LockObject { get; private set; }

        public SinkManager()
        {
            LockObject = new object();
        }

        public void RegisterAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes().Where(type => typeof(SinkBase).IsAssignableFrom(type) && type.GetCustomAttribute<SinkIdAttribute>() != null))
            {
                RegisterType(type);
            }
        }

        public void RegisterType(Type type)
        {
            // Exception if already registered..
            _sinkTypes.Add(GetSinkFourCc(type), type);
        }

        public IEnumerable<T> SinksOfType<T>() where T : ISink
        {
            lock (LockObject)
            {
                return _sinks.OfType<T>().ToArray();
            }
        }

        internal static string GetSinkFourCc(Type type)
        {
            return type.GetCustomAttribute<SinkIdAttribute>().SinkFourCc;
        }

        public SinkBase CreateSink(string sinkFourCc, ITcpNode node, int index)
        {
            Type type;
            if (!_sinkTypes.TryGetValue(sinkFourCc, out type))
            {
                // Unknown sink type
                Logger.Warning("Unknown sink code", "code", sinkFourCc, "guid", node.NodeId);
                return null;
            }

            var sink = (SinkBase)Activator.CreateInstance(type);
            sink.Init(node, index);

            lock (LockObject)
            {
                // Communicate new sink to API
                _sinks.Add(sink);
                if (CollectionChanged != null)
                {
                    CollectionChanged(this, new CollectionChangeEventArgs(CollectionChangeAction.Add, sink));
                }
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
                    if (CollectionChanged != null)
                    {
                        CollectionChanged(this, new CollectionChangeEventArgs(CollectionChangeAction.Remove, sink));
                    }
                }
                else
                {
                    throw new InvalidOperationException("Unexpected sink remove");
                }
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

        public event EventHandler<CollectionChangeEventArgs> CollectionChanged;
    }
}
