using System;
using System.Threading.Tasks;
using Lucky.Home.Devices;
using Lucky.Home.Protocol;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Base class for sinks
    /// </summary>
    internal class SinkBase : IDisposable, ISink
    {
        private int _index;

        public SinkBase()
        {
            SubCount = 0;
        }

        public void Init(ITcpNode node, int index)
        {
            Node = node;
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
                return new SinkPath(Node.Id, FourCc);
            }
        }

        public ITcpNode Node { get; private set; }

        public string FourCc
        {
            get
            {
                return SinkManager.GetSinkFourCc(GetType());
            }
        }

        protected Task Read(Action<IConnectionReader> readHandler)
        {
            if (Node != null)
            {
                return Node.ReadFromSink(_index, readHandler);
            }
            else
            {
                throw new InvalidOperationException("Node not found/unregistered");
            }
        }

        protected Task Write(Action<IConnectionWriter> writeHandler)
        {
            if (Node != null)
            {
                return Node.WriteToSink(_index, writeHandler);
            }
            else
            {
                throw new InvalidOperationException("Node not found/unregistered");
            }
        }

        public int SubCount { get; protected set; }
    }
}
