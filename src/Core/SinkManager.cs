using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Lucky.Home.Core
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class SinkManager : ServiceBase
    {
        private readonly Dictionary<string, Type> _sinkTypes = new Dictionary<string, Type>();
        private readonly ObservableCollection<ISink> _sinks = new ObservableCollection<ISink>();
 
        public SinkManager()
            : base("SinkManager")
        { }

        public void RegisterAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes().Where(type => typeof(Sink).IsAssignableFrom(type) && type.GetCustomAttribute<SinkIdAttribute>() != null))
            {
                // Exception if already registered..
                _sinkTypes.Add(GetSinkFourCc(type), type);
            }
        }

        public IEnumerable<T> SinksOfType<T>() where T : ISink
        {
            lock (_sinks)
            {
                return _sinks.OfType<T>().ToArray();
            }
        }

        internal static string GetSinkFourCc(Type type)
        {
            return type.GetCustomAttribute<SinkIdAttribute>().SinkFourCC;
        }

        public Sink CreateSink(string sinkFourCc, Guid ndoeGuid, int index)
        {
            Type type;
            if (!_sinkTypes.TryGetValue(sinkFourCc, out type))
            {
                // Unknown sink type
                Logger.Warning("Unknown sink code", "code", sinkFourCc, "guid", ndoeGuid);
                return null;
            }

            var sink = (Sink)Activator.CreateInstance(type);
            sink.NodeGuid = ndoeGuid;
            sink.Index = index;

            lock (_sinks)
            {
                _sinks.Add(sink);
            }
            return sink;
        }
    }
}
