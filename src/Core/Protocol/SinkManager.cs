//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using Lucky.Home.Sinks;

//namespace Lucky.Home.Core
//{
//    class SinkManager : ServiceBase
//    {
//        private readonly Dictionary<SinkTypes, Type> _sinkTypes = new Dictionary<SinkTypes, Type>();

//        internal void RegisterSinkDevice<T>(SinkTypes sinkType) where T : Sink, new()
//        {
//            // Exception if already registered..
//            _sinkTypes.Add(sinkType, typeof(T));
//        }

//        internal void RegisterAssembly(Assembly assembly)
//        {
//            foreach (Type type in assembly.GetTypes().Where(t => typeof(Sink).IsAssignableFrom(t)))
//            {
//                SinkIdAttribute[] attr = (SinkIdAttribute[])type.GetCustomAttributes(typeof(SinkIdAttribute), false);
//                if (attr.Length >= 1)
//                {
//                    _sinkTypes.Add(attr[0].SinkType, type);
//                }
//            }
//        }

//        public Sink CreateSink(SinkTypes sinkType)
//        {
//            Type type;
//            if (!_sinkTypes.TryGetValue(sinkType, out type))
//            {
//                // Unknown sink type
//                return null;
//            }
//            return (Sink)Activator.CreateInstance(type);
//        }
//    }
//}
