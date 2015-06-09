//using System;
//using System.Threading;
//using Lucky.Home.Core;
//using Lucky.Home.Core.Serialization;

//// ReSharper disable once UnusedMember.Global

//namespace Lucky.Home.Sinks
//{
//    /// <summary>
//    /// Simple display protocol
//    /// </summary>
//    /// <remarks>
//    /// Protocol: TCP_OPEN, 
//    ///          => [word:lenght] + ASCII
//    ///          <= [2] ErrorCode (0: OK)
//    ///           TCP_CLOSE
//    /// </remarks>
//    [SinkId(SinkTypes.Display)]
//    class DisplaySink : Sink
//    {
//        private Timer _timer;

//        public DisplaySink()
//        {
//            Schedule();
//        }

//        private void Schedule()
//        {
//            _timer = new Timer(SendHi, null, 500, Timeout.Infinite);
//        }

//        private class Message
//        {
//            [SerializeAsDynArray]
//            public string Text;
//        }

//        private void SendHi(object o)
//        {
//            try
//            {
//                using (var connection = Open())
//                {
//                    Message msg = new Message { Text = DateTime.Now.ToString("HH:mm:ss") };
//                    connection.Write(msg);
//                    ErrorCode ack = connection.Read<ErrorCode>();
//                    if (ack != ErrorCode.Ok)
//                    {
//                        ConsoleLogger.Log("Bad response  at " + this + ": " + ack);
//                        return;
//                    }

//                    //ushort ringStart = connection.Read<ushort>();
//                    //ushort ringEnd = connection.Read<ushort>();
//                    //ushort streamSize = connection.Read<ushort>();
//                    //Console.WriteLine("Ring: {0:x4}-{1:x4}, Wait: {2:x4}", ringStart, ringEnd, streamSize);
//                }
//                Schedule();
//            }
//            catch (Exception exc)
//            {
//                ConsoleLogger.Log("SocketException " + exc.Message + " at " + this);
//                _timer.Dispose();
//            }
//        }
//    }
//}
