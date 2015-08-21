using System;
using System.Threading.Tasks;
using Lucky.Home.Devices;
using Lucky.Home.Protocol;
using Lucky.Services;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Base class for sinks
    /// </summary>
    internal class SinkBase : IDisposable, ISink
    {
        private Guid _nodeGuid;
        private int _index;

        public void Init(Guid nodeGuid, int index)
        {
            _nodeGuid = nodeGuid;
            _index = index;
            OnInitialize();
        }

        protected virtual void OnInitialize()
        { }

        public virtual void Dispose()
        { }

        public SinkPath Path
        {
            get
            {
                return new SinkPath(_nodeGuid, FourCc);
            }
        }

        public ITcpNode Node
        {
            get
            {
                return Manager.GetService<INodeRegistrar>().FindNode(_nodeGuid);
            }
        }

        public string FourCc
        {
            get
            {
                return SinkManager.GetSinkFourCc(GetType());
            }
        }

        protected Task Read(Action<IConnectionReader> readHandler)
        {
            var node = Node;
            if (node != null)
            {
                return node.ReadFromSink(_index, readHandler);
            }
            else
            {
                throw new InvalidOperationException("Node not found/unregistered");
            }
        }

        protected Task Write(Action<IConnectionWriter> writeHandler)
        {
            var node = Manager.GetService<INodeRegistrar>().FindNode(_nodeGuid);
            if (node != null)
            {
                return node.WriteToSink(_index, writeHandler);
            }
            else
            {
                throw new InvalidOperationException("Node not found/unregistered");
            }
        }
    }
}
