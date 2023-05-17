using Lucky.Home.Models;
using Lucky.Home.Services;
using Lucky.Home.Simulator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Lucky.Home.Views
{
    /// <summary>
    /// Mock sink + UI for bit input array
    /// </summary>
    [MockSink("DIAR", "Input array")]
    public partial class DigitalInputArraySinkView : UserControl, ISinkMock
    {
        private ILogger Logger;
        private List<ChangesEvent> _events = new List<ChangesEvent>();

        private class ChangesEvent
        {
            public readonly DateTime TimeStamp = DateTime.Now;
            public byte[] State;
        }

        public DigitalInputArraySinkView()
        {
            InitializeComponent();

            SwitchesCount = 8;
            DataContext = this;
        }

        public void Init(ISimulatedNode node)
        {
            Logger = Manager.GetService<ILoggerFactory>().Create("DiarSink", node.Id.ToString());
            node.IdChanged += (o, e) => Logger.SubKey = node.Id.ToString();
        }

        public void Read(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        public void Write(BinaryWriter writer)
        {
            // Write back events
            Dispatcher.Invoke(() =>
            {
                var count = SwitchesCount;
                // Write count (high nibble) + tick size
                writer.Write((byte)((count << 4) + sizeof(long)));
                // Write current state
                writer.Write(GetState());

                // Write ticks/seconds in long
                writer.Write((long)10e6);
                // Write now in ticks
                writer.Write(DateTime.Now.Ticks);

                lock (_events)
                {
                    // Write event count (1 byte)
                    int n = Math.Min(_events.Count, 255);
                    writer.Write((byte)n);
                    foreach (var evt in _events.Take(n))
                    {
                        // Write timestamp
                        writer.Write(evt.TimeStamp.Ticks);
                        // Write state
                        writer.Write(evt.State);
                    }
                    _events.RemoveRange(0, n);
                }
            });
        }

        public static readonly DependencyProperty SwitchesCountProperty = DependencyProperty.Register(
            "SwitchesCount", typeof(int), typeof(DigitalInputArraySinkView), new PropertyMetadata(default(int), HandleSwitchesCountChanged));

        public int SwitchesCount
        {
            get { return (int)GetValue(SwitchesCountProperty); }
            set { SetValue(SwitchesCountProperty, value); }
        }

        private byte[] GetState()
        {
            int n = Inputs.Count;
            int bytes = ((n - 1) / 8) + 1;
            byte[] ret = new byte[bytes];
            for (int i = 0; i < n; i++)
            {
                // Pack bits
                ret[i / 8] = (byte)(ret[i / 8] | (Inputs[i].Value ? (1 << (i % 8)) : 0));
            }
            return ret;
        }

        private static void HandleSwitchesCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DigitalInputArraySinkView me = (DigitalInputArraySinkView)d;
            me.Inputs = new ObservableCollection<Switch>(Enumerable.Range(0, me.SwitchesCount).Select(i =>
            {
                var model = new Switch(true, "Input " + i);
                model.ValueChanged += (o, _) =>
                {
                    // Called on UI thread, OK to fetch all values here
                    lock (me._events)
                    {
                        me._events.Add(new ChangesEvent { State = me.GetState() });
                    }
                };
                return model;
            }));
            me._events.Clear();
        }

        public static readonly DependencyProperty InputsProperty = DependencyProperty.Register(
            "Inputs", typeof(ObservableCollection<Switch>), typeof(DigitalInputArraySinkView), new PropertyMetadata(default(ObservableCollection<Switch>)));

        public ObservableCollection<Switch> Inputs
        {
            get { return (ObservableCollection<Switch>)GetValue(InputsProperty); }
            set { SetValue(InputsProperty, value); }
        }
    }
}
