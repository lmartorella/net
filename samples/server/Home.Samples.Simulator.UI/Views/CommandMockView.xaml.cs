using Lucky.Home.Models;
using Lucky.Home.Services;
using Lucky.Home.Simulator;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Lucky.Home.Views
{
    [MockSink("TCMD")]
    public partial class CommandMockView : UserControl, ISinkMock
    {
        public ILogger Logger { get; set; }

        public CommandMockView()
        {
            InitializeComponent();

            SendCommandCommand = new UiCommand(() =>
            {
                LastCommand = Command;
                Command = "";
                CommandLog += "<- " + LastCommand + Environment.NewLine;
            });
        }

        public string LastCommand { get; private set; }

        public void Read(BinaryReader reader)
        {
            // Read response to command
            var l = reader.ReadInt16();
            var response = Encoding.UTF8.GetString(reader.ReadBytes(l));
            Dispatcher.Invoke(() =>
            {
                CommandLog += "-> " + response + Environment.NewLine;
            });
        }

        public void Write(BinaryWriter writer)
        {
            var cmd = LastCommand ?? "";
            writer.Write((short)cmd.Length);
            writer.Write(Encoding.UTF8.GetBytes(cmd));
            LastCommand = null;
        }

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof(string), typeof(CommandMockView), new PropertyMetadata(default(string)));

        public string Command
        {
            get { return (string)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly DependencyProperty CommandLogProperty = DependencyProperty.Register(
            "CommandLog", typeof(string), typeof(CommandMockView), new PropertyMetadata(default(string)));

        public string CommandLog
        {
            get { return (string)GetValue(CommandLogProperty); }
            set { SetValue(CommandLogProperty, value); }
        }

        public static readonly DependencyProperty SendCommandCommandProperty = DependencyProperty.Register(
            "SendCommandCommand", typeof(UiCommand), typeof(CommandMockView), new PropertyMetadata(default(UiCommand)));

        public UiCommand SendCommandCommand
        {
            get { return (UiCommand)GetValue(SendCommandCommandProperty); }
            set { SetValue(SendCommandCommandProperty, value); }
        }
    }
}
