using System;
using System.ComponentModel;
using Lucky.Home.Services;

namespace Lucky.Home.Sinks
{
    internal interface ISinkManager : IService
    {
        object LockObject { get; }

        event EventHandler<CollectionChangeEventArgs> CollectionChanged;

        void RaiseResetSink(SinkBase sinkBase);
    }
}
