using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lucky.Home.Core
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class SinkManager : ServiceBase
    {
        private readonly Dictionary<string, Type> _sinkTypes = new Dictionary<string, Type>();

        public SinkManager()
            : base("SinkManager")
        { }

        internal void RegisterSinkDevice<T>(string sinkType) where T : Sink, new()
        {
            // Exception if already registered..
            _sinkTypes.Add(sinkType, typeof(T));
        }

        internal void RegisterAssembly(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes().Where(t => typeof(Sink).IsAssignableFrom(t)))
            {
                SinkIdAttribute[] attr = (SinkIdAttribute[])type.GetCustomAttributes(typeof(SinkIdAttribute), false);
                if (attr.Length >= 1)
                {
                    _sinkTypes.Add(attr[0].SinkFourCC, type);
                }
            }
        }

        public Sink CreateSink(string sinkFourCc)
        {
            Type type;
            if (!_sinkTypes.TryGetValue(sinkFourCc, out type))
            {
                // Unknown sink type
                return null;
            }
            return (Sink)Activator.CreateInstance(type);
        }
    }
}
