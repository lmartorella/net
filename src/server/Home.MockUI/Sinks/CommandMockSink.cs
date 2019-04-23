using System;
using System.IO;
using System.Text;

namespace Lucky.HomeMock.Sinks
{
    class CommandMockSink : SinkMockBase
    {
        private MainWindow _owner;

        public CommandMockSink(MainWindow owner) 
            :base("TCMD")
        {
            _owner = owner;
            _owner.SendCommandCommand = new UiCommand(() =>
            {
                LastCommand = _owner.Command;
                _owner.Command = "";
                _owner.CommandLog += "<- " + LastCommand + Environment.NewLine;
            });
        }

        public string LastCommand { get; private set; }

        public override void Read(BinaryReader reader)
        {
            // Read response to command
            var l = reader.ReadInt16();
            var response = Encoding.UTF8.GetString(reader.ReadBytes(l));
            _owner.Dispatcher.Invoke(() => 
            {
                _owner.CommandLog += "-> " + response + Environment.NewLine;
            });
        }

        public override void Write(BinaryWriter writer)
        {
            var cmd = LastCommand ?? "";
            writer.Write((short)cmd.Length);
            writer.Write(Encoding.UTF8.GetBytes(cmd));
            LastCommand = null;
        }
    }
}
