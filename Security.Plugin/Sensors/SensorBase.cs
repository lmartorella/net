using System;

namespace Lucky.Home.Security.Sensors
{
    internal abstract class SensorBase
    {
        private NodeStatus _status = NodeStatus.None;

        protected SensorBase(string displayName)
        {
            DisplayName = displayName;
        }

        /// <summary>
        /// Custom display name
        /// </summary>
        public string DisplayName { get; private set;  }

        public bool IsArmed { get; set; }

        /// <summary>
        /// Current status
        /// </summary>
        public NodeStatus Status
        {
            get
            {
                return _status;
            }
            protected set
            {
                if (_status != value)
                {
                    _status = value;
                    if (StatusChanged != null)
                    {
                        StatusChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// Event raised when status changes
        /// </summary>
        public event EventHandler StatusChanged;
    }
}