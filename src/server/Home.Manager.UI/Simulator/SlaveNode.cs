using System;
using System.Linq;
using Lucky.Home.Services;

namespace Lucky.Home.Simulator
{
    public class SlaveNode : ISimulatedNode
    {
        public ILogger Logger { get; private set; }
        public ISinkMock[] Sinks { get; }

        public IStateProvider StateProvider { get; private set; }

        public SlaveNode(ILogger logger, IStateProvider stateProvider, string[] sinks)
        {
            Logger = logger;
            StateProvider = stateProvider;
            var sinkManager = Manager.GetService<MockSinkManager>();
            Sinks = sinks.Select(name => sinkManager.Create(name, this)).ToArray();
        }

        public Guid Id
        {
            get
            {
                return StateProvider.Id;
            }
            set
            {
                StateProvider.Id = value;
            }
        }
    }
}
