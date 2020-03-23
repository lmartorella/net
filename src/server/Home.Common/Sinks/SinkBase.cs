using System;
using Lucky.Home.Devices;
using Lucky.Home.Protocol;
using Lucky.Home.Services;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Base class for sinks
    /// </summary>
    public class SinkBase : IDisposable, ISink
    {
        private int _index;
        protected ILogger Logger;
        private Task _initialized;

        public SinkBase(string fourcc = null)
        {
            Logger = Manager.GetService<LoggerFactory>().Create(GetType().Name);
            SubCount = 0;
            FourCc = fourcc ?? SinkTypeManager.GetSinkFourCc(GetType());
        }

        internal void Init(ITcpNode node, int index)
        {
            Node = node;
            _index = index;

            Logger = Manager.GetService<LoggerFactory>().Create(GetType().Name + ":" + node.Address);

            // Start initialization, don't wait for async
            _initialized = OnInitialize();
        }

        protected virtual Task OnInitialize()
        {
            return Task.CompletedTask;
        }

        public Task Initialized
        {
            get
            {
                return _initialized;
            }
        }

        public virtual void Dispose()
        { }

        internal SinkPath Path
        {
            get
            {
                return new SinkPath(Node.NodeId, FourCc);
            }
        }

        public bool IsOnline
        {
            get
            {
                return Node != null && !Node.IsZombie;
            }
        }

        internal ITcpNode Node { get; private set; }

        public override string ToString()
        {
            return GetType().Name + "[" + Node.NodeId + "]";
        }

        public string FourCc { get; private set; }

        /// <summary>
        /// Read data after connection is established
        /// </summary>
        protected Task<bool> Read(Func<IConnectionReader, Task> readHandler, int timeout = 0, [CallerMemberName] string context = "")
        {
            if (Node != null)
            {
                return Node.ReadFromSink(_index, readHandler, timeout, context);
            }
            else
            {
                throw new InvalidOperationException("Node not found/unregistered");
            }
        }

        /// <summary>
        /// Write data after connection is established
        /// </summary>
        protected Task<bool> Write(Func<IConnectionWriter, Task> writeHandler, [CallerMemberName] string context = "")
        {
            if (Node != null)
            {
                return Node.WriteToSink(_index, writeHandler, context);
            }
            else
            {
                throw new InvalidOperationException("Node not found/unregistered");
            }
        }

        /// <summary>
        /// Count of sub sinks
        /// </summary>
        public int SubCount { get; protected set; }

        /// <summary>
        /// Do a system reset of the node that exposes the sink
        /// </summary>
        public void ResetNode()
        {
            Manager.GetService<ISinkManager>().RaiseResetSink(this);
        }
    }
}
