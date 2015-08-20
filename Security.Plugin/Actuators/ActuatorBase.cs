namespace Lucky.Home.Security.Actuators
{
    internal abstract class ActuatorBase
    {
        protected ActuatorBase(string displayName)
        {
            DisplayName = displayName;
        }

        public string DisplayName { get; private set; }

        public NodeStatus Status { get; set; }
    }
}