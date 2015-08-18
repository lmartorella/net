namespace Lucky.Home.Security.Actuators
{
    internal interface IActuator
    {
        string DisplayName { get; }
        void Trigger();
        void Disable();
    }
}