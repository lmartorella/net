using System;
using System.Collections.Generic;
using System.Linq;
using Lucky.Home.Sinks;

// ReSharper disable once UnusedMember.Global

namespace Lucky.Home.Devices
{
    /// <summary>
    /// Switch device. All outputs are set to the XOR of all inputs, like
    /// classic electric wall switches.
    /// Needs at least one input (Digital Input Array) and one output (Digital Out Array)
    /// </summary>
    [Device(new[] { typeof(IDigitalInputArraySink), typeof(IDigitalOutputArraySink) })]
    internal class SwitchDevice : DeviceBase, ISwitchDevice
    {
        private readonly TimeSpan _period;

        private readonly List<SubSink<IDigitalInputArraySink>> _inputs = new List<SubSink<IDigitalInputArraySink>>();
        private readonly List<SubSink<IDigitalOutputArraySink>> _outputs = new List<SubSink<IDigitalOutputArraySink>>();

        private bool _lastStatus;

        public SwitchDevice()
        {
            _period = TimeSpan.FromMilliseconds(250);
        }

        protected override void OnSinkChanged(SubSink removed, SubSink added)
        {
            base.OnSinkChanged(removed, added);

            bool updated = false;
            if (removed != null)
            {
                var removedInput = removed.Sink as IDigitalInputArraySink;
                if (removedInput != null)
                {
                    removedInput.StatusChanged -= HandleStatusChanged;
                    _inputs.Remove(removed);
                }
                else
                {
                    _outputs.Remove(removed);
                }
                updated = true;
            }

            if (added != null)
            {
                var addedInput = added.Sink as IDigitalInputArraySink;
                if (addedInput != null)
                {
                    addedInput.StatusChanged += HandleStatusChanged;
                    // TODO
                    addedInput.PollPeriod = _period;
                    _inputs.Add(added);
                }
                else
                {
                    _outputs.Add(added);
                }
                updated = true;
            }

            if (updated)
            {
                UpdateOutput();
                HandleStatusChanged(null, null);
            }
        }

        private void HandleStatusChanged(object sender, EventArgs e)
        {
            Status = _inputs.Aggregate(false, (c, input) => c ^ (input.Sink.Status.Length > input.SubIndex && input.Sink.Status[input.SubIndex]));
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var input in _inputs)
            {
                input.Sink.StatusChanged -= HandleStatusChanged;
            }
            base.Dispose(disposing);
        }

        public bool Status
        {
            get 
            {
                return _lastStatus;
            }
            private set
            {
                if (_lastStatus != value)
                {
                    _lastStatus = value;
                    UpdateOutput();
                    if (StatusChanged != null)
                    {
                        StatusChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        private void UpdateOutput()
        {
            foreach (var output in _outputs)
            {
                var state = output.Sink.Status;
                state[output.SubIndex] = Status;
                output.Sink.Status = state;
            }
        }

        public event EventHandler StatusChanged;
    }
}