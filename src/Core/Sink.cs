using System;
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
        
        protected byte[] ReadBytes()
        {
            var node = Manager.GetService<NodeRegistrar>().FindNode(_nodeGuid);
            if (node != null)
            {
                return node.ReadFromSink(_index);
            }
            else
            {
                return null;
            }
        }

        protected bool WriteBytes(byte[] data)
        {
            var node = Manager.GetService<NodeRegistrar>().FindNode(_nodeGuid);
            if (node != null)
            {
                return node.WriteToSink(data, _index);
            }
            else
            {
                return false;
            }
        }
    }
}
