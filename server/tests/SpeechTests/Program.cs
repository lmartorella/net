using System.Globalization;
using System.IO;
using Lucky.Home.Speech;
using Lucky.Services;

namespace SpeechTests
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = Manager.GetService<TextToSpeechService>();
            service.CultureInfo = new CultureInfo("it-IT");
            using (var reader = service.TextToAudio("This is a test"))
            {
                using (var writer = new FileStream(args[0], FileMode.Create, FileAccess.Write))
                {
                    reader.CopyTo(writer);
                }
            }
        }
    }
}
