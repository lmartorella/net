namespace Lucky.Home.Devices
{
    internal interface IDeviceInternal : IDevice
    {
        void OnInitialize(string argument, SinkPath sinkPath);
    }
}