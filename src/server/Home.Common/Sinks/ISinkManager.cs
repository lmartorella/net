using System;
using System.ComponentModel;
using Lucky.Home.Devices;
using Lucky.Home.Services;

namespace Lucky.Home.Sinks
{
    internal interface ISinkManager : IService
    {
        object LockObject { get; }

        event EventHandler<CollectionChangeEventArgs> CollectionChanged;

        ISink FindOwnerSink(SinkPath sinkPath);

        void RaiseResetSink(SinkBase sinkBase);
    }
}
