using System;
using System.Collections.Generic;
using System.Linq;
using Lucky.Home.Application;
using Lucky.Home.Security.Actuators;
using Lucky.Home.Security.Sensors;

namespace Lucky.Home.Security
{
    class SecurityPlugin : AppBase
    {
        private readonly ActuatorBase[] _actuators;
        private readonly SensorBase[] _sensors;

        private readonly Dictionary<SensorBase, bool> _engagedSensors = new Dictionary<SensorBase, bool>();
        
        private bool _isEngaged;
        private AlarmStatus _alarmStatus;
        private SensorBase _nodeInPrealarm ;

        public SecurityPlugin(IEnumerable<ActuatorBase> actuators, IEnumerable<SensorBase> sensors)
        {
            _actuators = actuators.ToArray();
            _sensors = sensors.ToArray();
            IsEngaged = false;
            AlarmStatus = AlarmStatus.Unarmed;
        }

        protected override void Dispose(bool disposing)
        {
            Disengage();
            base.Dispose(disposing);
        }

        private void Engage()
        {
            foreach (var tuple in _engagedSensors.Keys)
            {
                tuple.IsArmed = true;
                tuple.StatusChanged += HandleSwitchChanged;
            }
            _nodeInPrealarm = null;
        }

        private void Disengage()
        {
            foreach (var tuple in _engagedSensors.Keys)
            {
                tuple.StatusChanged -= HandleSwitchChanged;
                tuple.IsArmed = false;
            }
            foreach (var actuator in _actuators)
            {
                actuator.Status = NodeStatus.Normal;
            }
            _engagedSensors.Clear();
        }

        private void HandleSwitchChanged(object sender, EventArgs e)
        {
            SensorBase ds = (SensorBase)sender;

            var nodeStatus = ds.Status;
            if (_engagedSensors[ds])
            {
                // Node started in pre-alarm. Treat "normal/prealarm" transitions as ignored
                if (nodeStatus == NodeStatus.PreAlarm)
                {
                    nodeStatus = NodeStatus.Normal;
                }
            }

            switch (nodeStatus)
            {
                case NodeStatus.Normal:
                    if (ds == _nodeInPrealarm)
                    {
                        // Disable prealarm
                        if (AlarmStatus == AlarmStatus.PreAlarm)
                        {
                            AlarmStatus = AlarmStatus.Armed;
                        }
                        _nodeInPrealarm = null;
                    }
                    break;
                case NodeStatus.PreAlarm:
                    // Alarm if 2 or more in prealarm
                    switch (AlarmStatus)
                    {
                        case AlarmStatus.PreAlarm:
                            if (ds != _nodeInPrealarm)
                            {
                                // Go in alarm state
                                AlarmStatus = AlarmStatus.Alarm;
                            }
                            break;
                        case AlarmStatus.Armed:
                            _nodeInPrealarm = ds;
                            AlarmStatus = AlarmStatus.PreAlarm;
                            break;
                    }
                    break;
                case NodeStatus.Offline:
                case NodeStatus.Alarm:
                    // Go in alarm state
                    AlarmStatus = AlarmStatus.Alarm;
                    break;
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
                        AlarmStatus = AlarmStatus.Armed;
                        // Do not subscribe opened switches, they can float during the engaged state
                        GetValidSensors();
                        Engage();
                    }
                    else
                    {
                        AlarmStatus = AlarmStatus.Unarmed;
                        Disengage();
                    }
                }
            }
        }

        private void GetValidSensors()
        {
            // Node in alarm will be disabled!
            foreach (var sensor in _sensors.Where(s => s.Status == NodeStatus.Normal || s.Status == NodeStatus.PreAlarm))
            {
                _engagedSensors[sensor] = sensor.Status == NodeStatus.PreAlarm;
            }
        }

        public IEnumerable<SensorBase> GetSensorInError()
        {
            return _sensors.Where(s => s.Status == NodeStatus.Offline|| s.Status == NodeStatus.PreAlarm || s.Status == NodeStatus.Alarm);
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

                    foreach (var actuator in _actuators)
                    {
                        actuator.Status = Cvt(AlarmStatus);
                    }
                }
            }
        }

        private static NodeStatus Cvt(AlarmStatus alarmStatus)
        {
            switch (alarmStatus)
            {
                case AlarmStatus.Unarmed:
                case AlarmStatus.Armed:
                    return NodeStatus.Normal;
                case AlarmStatus.PreAlarm:
                    return NodeStatus.PreAlarm;
                case AlarmStatus.Alarm:
                    return NodeStatus.Alarm;
                default:
                    throw new ArgumentOutOfRangeException("alarmStatus", alarmStatus, null);
            }
        }

        public event EventHandler AlarmStatusChanged;
    }
}
