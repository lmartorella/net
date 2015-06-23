using System;
using System.IO;
using System.Runtime.CompilerServices;
using Lucky.Home.Core;
using Lucky.Home.Core.Protocol;
using Lucky.Home.Speech;

[assembly: InternalsVisibleTo("SpeechTests")]

namespace Lucky.Home
{
    static class Program
    {
        static void Main(string[] args)
        {
            Manager.Register<Server, IServer>();
            Manager.Register<NodeRegistrar, INodeRegistrar>();
            //Manager.Register<SinkManager, SinkManager>();
            //Manager.GetService<SinkManager>().RegisterAssembly(Assembly.GetExecutingAssembly());

            // Start server
            Manager.GetService<IServer>();

            while (true)
            {
                Console.Write("Enter 's' to start stream test, 't' to speech, or 'q' to quit: ");
                string str = Console.ReadLine();
                switch (str)
                {
                    case "q":
                        return;
                    case "s":
                        using (var fileStream = new FileInfo(args[0]).OpenRead())
                        {
                            //AudioPlayerSink.DoStartFileTest(fileStream);
                        }
                        break;
                    case "t":
                        string text;
                        using (var streamReader = new FileInfo(args[1]).OpenText())
                        {
                            text = streamReader.ReadToEnd();
                        }
                        using (var stream = Manager.GetService<TextToSpeechService>().TextToAudio(text))
                        {
                            //AudioPlayerSink.DoStartFileTest(stream);
                        }
                        break;
                }
            }
        }
    }
}
