using Lucky.Home.Simulator;
using Lucky.Home.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lucky.Home.Services
{
    class MockSinkManager : ServiceBase
    {
        private readonly Dictionary<string, Type> _mockTypes = new Dictionary<string, Type>();

        public MockSinkManager()
        {
            _mockTypes.Add(GetFourCc(typeof(SystemSinkView)), typeof(SystemSinkView));
        }

        public void RegisterAssembly(Assembly assembly)
        {
            Type[] sinkTypes = assembly.GetTypes().Where(t => typeof(ISinkMock).IsAssignableFrom(t) && t.GetCustomAttribute<MockSinkAttribute>() != null).ToArray();
            foreach (Type type in sinkTypes)
            {
                var fourcc = type.GetCustomAttribute<MockSinkAttribute>().FourCc;
                _mockTypes.Add(fourcc, type);
            }            
        }

        public ISinkMock Create(string fourcc, ISimulatedNode node)
        {
            var sink = (ISinkMock)Activator.CreateInstance(_mockTypes[fourcc], node);
            sink.Logger = Logger;
            return sink;
        }

        internal string[] GetAllSinks()
        {
            return _mockTypes.Keys.ToArray();
        }

        internal string GetFourCc(Type sinkType)
        {
            return sinkType.GetCustomAttribute<MockSinkAttribute>().FourCc;
        }

        internal string GetFourCc(ISinkMock sink)
        {
            return GetFourCc(sink.GetType());
        }
    }
}
