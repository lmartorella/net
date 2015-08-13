using System;
using System.Threading.Tasks;
using Lucky.Home.Protocol;

namespace Lucky.Home.Core
{
    /// <summary>
    /// Base class for sinks
    /// </summary>
    internal class Sink : IDisposable, ISink
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

        public string FourCc
        {
            get
            {
                return SinkManager.GetSinkFourCc(GetType());
            }
        }
        
        protected Task<bool> Read(Action<IConnectionReader> readHandler)
        {
            var node = Manager.GetService<NodeRegistrar>().FindNode(_nodeGuid);
            if (node != null)
            {
                return node.ReadFromSink(_index, readHandler);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        protected Task<bool> Write(Action<IConnectionWriter> writeHandler)
        {
            var node = Manager.GetService<NodeRegistrar>().FindNode(_nodeGuid);
            if (node != null)
            {
                return node.WriteToSink(_index, writeHandler);
            }
            else
            {
                return Task.FromResult(false);
            }
        }
    }
}
