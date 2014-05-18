using System;
using System.Threading;
using System.Threading.Tasks;
using Lucky.Home.Core;
using Lucky.Home.Core.Serialization;
using Lucky.Home.Resources;

// ReSharper disable once UnusedMember.Global

namespace Lucky.Home.Sinks
{
    [DeviceId(AudioSinkId)]
    class AudioPlayerSink : Sink
    {
        private const int AudioSinkId = 3;

        private enum Command : byte
        {
            Init = 0,
            SetVolume = 1,
            TestSine = 2,
            StreamData = 102
        }

        private enum ErrorCode : byte
        {
            Ok = 0,
            HwFail = 1,      // MP3 hw fail (wrong model, internal test failed...)
            SocketErr = 2,   // Missing data/buffer underrun on TCP/IP socket
        }

        private class SineTestMessage
        {
            public Command Command;
            public ushort Frequency;

            public SineTestMessage(ushort frequency)
            {
                Command = Command.TestSine;
                Frequency = frequency;
            }
        }

        private class SetVolumeMessage
        {
            public Command Command;
            public byte LeftAttenuation;
            public byte RightAttenuation;

            public SetVolumeMessage(int leftAttenuation, int rightAttenuation)
            {
                Command = Command.SetVolume;
                LeftAttenuation = (byte) leftAttenuation;
                RightAttenuation = (byte) rightAttenuation;
            }
        }

        private class StreamDataMessage
        {
            public Command Command;
            [SerializeAsDynArrayAttribute]
            public byte[] Data;

            public StreamDataMessage(byte[] data, int start, int l)
            {
                Command = Command.StreamData;
                Data = new byte[l];
                Array.Copy(data, start, Data, 0, l);
            }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Task.Factory.StartNew(() =>
            {
                using (var connection = Open())
                {
                    connection.Write(Command.Init);
                    ErrorCode ack = connection.Read<ErrorCode>();
                    if (ack != ErrorCode.Ok)
                    {
                        Logger.Log("Bad response  at " + this + ": " + ack);
                        return;
                    }
                }

                //SineTest();
                Mp3Test();
            });
        }

        private void Mp3Test()
        {
            using (var connection = Open())
            {
                byte[] data = ExampleData.PortalEndingTheme_mp3;
                int i = 0;
                DateTime ts = DateTime.Now;
                int tdelta = 0;

                while (i < data.Length)
                {
                    const int MAX_PACKET_SIZE = 2048;
                    int l = Math.Min(MAX_PACKET_SIZE, data.Length - i);

                    StreamDataMessage msg = new StreamDataMessage(data, i, l);
                    connection.Write(msg);

                    ErrorCode ack = connection.Read<ErrorCode>();
                    if (ack != ErrorCode.Ok)
                    {
                        Logger.Log("Bad response  at " + this + ": " + ack);
                        return;
                    }

                    i += l;
                    tdelta += l;

                    DateTime t = DateTime.Now;
                    if (t - ts > TimeSpan.FromSeconds(1))
                    {
                        Console.WriteLine("{0}KB, avg: {1}KB/s\n", i / 1024, tdelta);
                        ts = t;
                        tdelta = 0;
                    }
                }
            }
        }

        private void SineTest()
        {
            using (var connection = Open())
            {
                connection.Write(new SineTestMessage(1500));

                ErrorCode ack = connection.Read<ErrorCode>();
                if (ack != ErrorCode.Ok)
                {
                    Logger.Log("Bad response  at " + this + ": " + ack);
                    return;
                }
            }

            for (int i = 0; i < 20; i++)
            {
                if (!SetVolume((i + 10), (30 - i)))
                {
                    return;
                }
                Thread.Sleep(30);
            }
            for (int i = 20; i > 0; i--)
            {
                if (!SetVolume((i + 10), (30 - i)))
                {
                    return;
                }
                Thread.Sleep(30);
            }
        }

        private bool SetVolume(int left, int right)
        {
            using (var connection = Open())
            {
                connection.Write(new SetVolumeMessage(left, right));

                ErrorCode ack = connection.Read<ErrorCode>();
                if (ack != ErrorCode.Ok)
                {
                    Logger.Log("Bad response  at " + this + ": " + ack);
                    return false;
                }
            }
            return true;
        }
    }
}
