using System;
using Lucky.Home.Protocol;

namespace Lucky.Home.Core
{
    /// <summary>
    /// Base class for sinks
    /// </summary>
    internal class Sink : IDisposable, ISink
    {
        public virtual void Dispose()
        { }

        public Guid NodeGuid { get; set; }
        public int Index { get; set; }

        public string FourCc
        {
            get
            {
                return SinkManager.GetSinkFourCc(GetType());
            }
        }

        protected bool WriteBytes(byte[] data)
        {
            var node = Manager.GetService<NodeRegistrar>().FindNode(NodeGuid);
            if (node != null)
            {
                return node.WriteToSink(data, Index);
            }
            else
            {
                return false;
            }
        }
    }
}
