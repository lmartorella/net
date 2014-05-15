using System;
using System.Threading;
using System.Threading.Tasks;
using Lucky.Home.Core;

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
            TestSine = 2
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

                using (var connection = Open())
                {
                    connection.Write(new byte[] { 2, 0xdc, 5 });
                    //connection.Write(new SineTestMessage(1500));

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
            });
        }

        private bool SetVolume(int left, int right)
        {
            using (var connection = Open())
            {
                connection.Write(new byte[] { 1, (byte)left, (byte)right });
                //connection.Write(new SetVolumeMessage(left, right));

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
