namespace Lucky.Home.Devices
{
    internal interface IDeviceInternal : IDevice
    {
        void OnInitialize(SinkPath[] sinkPaths);
    }
}