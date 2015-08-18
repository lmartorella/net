using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lucky.Home.Plugin;
using Lucky.Home.Security.Actuators;
using Lucky.Home.Security.Sensors;

namespace Lucky.Home.Security
{
    class SecurityPlugin : HomePluginBase
    {
        private readonly List<IActuator> _actuators = new List<IActuator>();
        private readonly List<ISensor> _sensors = new List<ISensor>();
        private ISensor[] _engagedSensors = new ISensor[0];
        private bool _isEngaged;
        private AlarmStatus _alarmStatus;

        public SecurityPlugin(IEnumerable<IActuator> actuators, IEnumerable<ISensor> sensors)
        {
            _actuators.AddRange(actuators);
            _sensors.AddRange(sensors);
            IsEngaged = false;
            AlarmStatus = AlarmStatus.Stale;
        }

        public override void Dispose()
        {
            Disengage();
            base.Dispose();
        }

        private void SubscribeEvents(IEnumerable<ISensor> toEngage)
        {
            // Do not subscribe opened switches, they can float during the engaged state
            _engagedSensors = toEngage.ToArray();
            foreach (var doorSwitch in _engagedSensors)
            {
                doorSwitch.StatusChanged += HandleSwitchChanged;
            }
        }

        private void Disengage()
        {
            foreach (var doorSwitch in _engagedSensors)
            {
                doorSwitch.StatusChanged -= HandleSwitchChanged;
            }
            foreach (var actuator in _actuators)
            {
                actuator.Disable();
            }
            _engagedSensors = new ISensor[0];
        }

        private void HandleSwitchChanged(object sender, EventArgs e)
        {
            ISensor ds = (ISensor)sender;
            Debug.Assert(_engagedSensors.Count(s => s == ds) == 1);

            // Fire alarm, if the switch changed status, opened or closed
            FireAlarm();
        }

        private void FireAlarm()
        {
            AlarmStatus = AlarmStatus.Active;
            foreach (var actuator in _actuators)
            {
                actuator.Trigger();
            }
        }

        public bool IsEngaged
        {
            get
            {
                return _isEngaged;
            }
            set
            {
                if (value != _isEngaged)
                {
                    _isEngaged = value;
                    if (IsEngaged)
                    {
                        AlarmStatus = AlarmStatus.Engaged;
                        SubscribeEvents(GetValidSensors());
                    }
                    else
                    {
                        AlarmStatus = AlarmStatus.Stale;
                        Disengage();
                    }
                }
            }
        }

        private IEnumerable<ISensor> GetValidSensors()
        {
            return _sensors.Where(s => s.Status == SwitchStatus.Closed);
        }

        public AlarmStatus AlarmStatus
        {
            get
            {
                return _alarmStatus;
            }
            private set
            {
                if (_alarmStatus != value)
                {
                    _alarmStatus = value;
                    if (AlarmStatusChanged != null)
                    {
                        AlarmStatusChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        public event EventHandler AlarmStatusChanged;
    }
}
