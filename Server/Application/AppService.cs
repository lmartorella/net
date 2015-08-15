using System;
using System.Threading;
using System.Threading.Tasks;
using Lucky.Home.Core;
using Lucky.Home.Sinks;
using Lucky.Services;

namespace Lucky.Home.Application
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class AppService : ServiceBase
    {
        // ReSharper disable once NotAccessedField.Local
        private Timer _timer;

        //private void CommandLine()
        //{
            //while (true)
            //{
            //    Console.Write("Enter 's' to start stream test, 't' to speech, or 'q' to quit: ");
            //    string str = Console.ReadLine();
            //    switch (str)
            //    {
            //        case "q":
            //            return;
            //        case "s":
            //            using (var fileStream = new FileInfo(args[0]).OpenRead())
            //            {
            //                AudioPlayerSink.DoStartFileTest(fileStream);
            //            }
            //            break;
            //        case "t":
            //            string text;
            //            using (var streamReader = new FileInfo(args[1]).OpenText())
            //            {
            //                text = streamReader.ReadToEnd();
            //            }
            //            using (var stream = Manager.GetService<TextToSpeechService>().TextToAudio(text))
            //            {
            //                AudioPlayerSink.DoStartFileTest(stream);
            //            }
            //            break;
            //    }
            //}
        //}

        private void MonitorDisplays()
        {
            _timer = new Timer(state =>
            {
                foreach (var sink in Manager.GetService<SinkManager>().SinksOfType<IDisplaySink>())
                {
                    sink.Write("Time: " + DateTime.Now.ToLongTimeString());
                }
                _timer.Change(10000, Timeout.Infinite);
            }, null, 10000, Timeout.Infinite);
        }

        public void Run()
        {
            Task.Run(() => MonitorDisplays());
            WaitBreak();
        }

        private static void WaitBreak()
        {
            object lockObject = new object();
            Console.CancelKeyPress += (sender, args) =>
            {
                lock (lockObject)
                {
                    Monitor.Pulse(lockObject);
                }
            };
            lock (lockObject)
            {
                Monitor.Wait(lockObject);
            }
        }
    }
}
