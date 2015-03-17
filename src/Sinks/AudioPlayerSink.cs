using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Lucky.Home.Core;
using Lucky.Home.Core.Serialization;

// ReSharper disable once UnusedMember.Global

namespace Lucky.Home.Sinks
{
    [SinkId(SinkTypes.AudioPlayer)]
    public class AudioPlayerSink : Sink
    {
        private readonly TextWriter HighConsole = Console.Out; // StreamWriter.Null;

        public enum Command : byte
        {
            Init = 0,
            SetVolume = 1,
            TestSine = 2,
            StreamDataPacket = 3,
            EnableSdi = 4,
            StreamBegin = 5,
            StreamEnd = 6,

            Test1 = 100,
        }

        public enum ErrorCode : byte
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

        public class StreamDataMessage
        {
            public Command Command;
            [SerializeAsDynArrayAttribute]
            public byte[] Data;

            public StreamDataMessage(byte[] data, int start, int l)
            {
                Command = Command.StreamDataPacket;
                Data = new byte[l];
                Array.Copy(data, start, Data, 0, l);
            }
        }

        public class StreamResponse
        {
            public ErrorCode Result;
            public ushort ElapsedMs;
            public ushort CallsCount;
            public int BufferFreeSize;
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
                        Logger.Log("Bad Init response at " + this + ": " + ack);
                    }

                    connection.Write(Command.EnableSdi);
                    ack = connection.Read<ErrorCode>();
                    if (ack != ErrorCode.Ok)
                    {
                        Logger.Log("Bad EnableSdi response at " + this + ": " + ack);
                    }

                    connection.Write(new SetVolumeMessage(25, 25));
                    ack = connection.Read<ErrorCode>();
                    if (ack != ErrorCode.Ok)
                    {
                        Logger.Log("Bad SetVolume response at " + this + ": " + ack);
                    }
                }

                //SineTest();
                //Mp3Test();
            });
            StartTestEvent += Mp3Test;
        }

        private void Mp3Test(Stream mp3Stream)
        {
            using (var connection = Open())
            {
                byte[] data;
                data = new byte[mp3Stream.Length];
                mp3Stream.Read(data, 0, (int)mp3Stream.Length);

                int i = 0;
                DateTime ts = DateTime.Now;
                int tdelta = 0;
                int recvTimeAcc = 0;
                int recvCallsAcc = 0;
                int recvSamples = 0;
                int recvBufferSizeAcc = 0;

                connection.Write(Command.StreamBegin);
                HighConsole.Write("StreamBegin...");

                ErrorCode ack = connection.Read<ErrorCode>();
                if (ack != ErrorCode.Ok)
                {
                    Logger.Log("Bad StreamResponse response at " + this + ": " + ack);
                    return;
                }
                HighConsole.WriteLine("OK");

                while (i < data.Length)
                {
                    //Thread.Sleep(300);
                    const int MAX_PACKET_SIZE = 128;
                    int l = Math.Min(MAX_PACKET_SIZE, data.Length - i);

                    connection.Write(new StreamDataMessage(data, i, l));
                    HighConsole.Write("Msg[{0}]", l);

                    var resp = connection.Read<StreamResponse>();
                    if (resp.Result != ErrorCode.Ok)
                    {
                        Logger.Log("Bad StreamResponse response at " + this + ": " + resp.Result);
                        return;
                    }
                    HighConsole.WriteLine("OK");

                    recvTimeAcc += resp.ElapsedMs;
                    recvCallsAcc += resp.CallsCount;
                    recvBufferSizeAcc += resp.BufferFreeSize;
                    recvSamples++;
                    i += l;
                    tdelta += l;

                    DateTime t = DateTime.Now;
                    if (t - ts > TimeSpan.FromSeconds(1))
                    {
                        Console.WriteLine("{0}KB, avg: {1:0.0}KB/s. MTU:1500: avgRcv: {2:0.0}ms, avgDequeue#: {3:0.0}, avgBufferFree: {4:0.0}Kb\n", i / 1024, tdelta / 1024.0, (float)recvTimeAcc / recvSamples, (float)recvCallsAcc / recvSamples, (float)recvBufferSizeAcc / recvSamples / 1024);
                        ts = t;
                        tdelta = 0;
                        recvTimeAcc = 0;
                        recvCallsAcc = 0;
                        recvBufferSizeAcc = 0;
                        recvSamples = 0;
                    }
                }

                connection.Write(Command.StreamEnd);
                ack = connection.Read<ErrorCode>();
                if (ack != ErrorCode.Ok)
                {
                    Logger.Log("Bad StreamResponse response at " + this + ": " + ack);
                    return;
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
                    Logger.Log("Bad response SetVolumeMessage at " + this + ": " + ack);
                    return false;
                }
            }
            return true;
        }

        private static event Action<Stream> StartTestEvent;

        public static void DoStartFileTest(Stream mp3Stream)
        {
            if (StartTestEvent != null)
            {
                StartTestEvent(mp3Stream);
            }
        }
    }
}
