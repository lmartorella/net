﻿using System;
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
    /// <summary>
    /// Sink manager
    /// </summary>
    class SinkManager : ServiceBase
    {
        private readonly Dictionary<string, Type> _sinkTypes = new Dictionary<string, Type>();
        private readonly ObservableCollection<SinkBase> _sinks = new ObservableCollection<SinkBase>();
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

        internal void RaiseResetSink(SinkBase sink)
        {
            ResetSink?.Invoke(this, new ResetSinkEventArgs(sink));
        }
    }
}
