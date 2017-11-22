﻿using System;
using Lucky.Home.Devices;
using Lucky.Home.Protocol;
using Lucky.Services;
using System.Runtime.CompilerServices;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Base class for sinks
    /// </summary>
    public class SinkBase : IDisposable, ISinkInternal
    {
        private int _index;
        protected ILogger Logger;

        public SinkBase(string fourcc = null)
        {
            Logger = Manager.GetService<LoggerFactory>().Create(GetType().Name);
            SubCount = 0;
            FourCc = fourcc ?? SinkManager.GetSinkFourCc(GetType());
        }

        internal void Init(ITcpNode node, int index)
        {
            Node = node;
            _index = index;

            Logger = Manager.GetService<LoggerFactory>().Create(GetType().Name + ":" + node.Address);

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
                return new SinkPath(Node.NodeId, FourCc);
            }
        }

        ITcpNode ISinkInternal.Node
        {
            get
            {
                return Node;

            }
        }

        internal ITcpNode Node { get; private set; }

        public string FourCc { get; private set; }

        protected bool Read(Action<IConnectionReader> readHandler, [CallerMemberName] string context = "")
        {
            if (Node != null)
            {
                return Node.ReadFromSink(_index, readHandler, context);
            }
            else
            {
                throw new InvalidOperationException("Node not found/unregistered");
            }
        }

        protected bool Write(Action<IConnectionWriter> writeHandler, [CallerMemberName] string context = "")
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

        public int SubCount { get; protected set; }
    }
}
