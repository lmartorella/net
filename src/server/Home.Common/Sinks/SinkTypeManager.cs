using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lucky.Home.Services;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Sink .NET type manager 
    /// </summary>
    class SinkTypeManager : ServiceBase
    {
        private readonly Dictionary<string, Type> _sinkTypes = new Dictionary<string, Type>();

        public Type FindType(string sinkFourCc)
        {
            Type type;
            _sinkTypes.TryGetValue(sinkFourCc, out type);
            return type;
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

        internal static string GetSinkFourCc(Type type)
        {
            return type.GetCustomAttribute<SinkIdAttribute>().SinkFourCc;
        }
    }
}
