using System.Globalization;
using System.IO;
using System.Speech.Synthesis;
using Lucky.Services;
using NAudio.Lame;
using NAudio.Wave;

// ReSharper disable once ClassNeverInstantiated.Global

namespace Lucky.Home.Speech
{
    class TextToSpeechService : ServiceBase
    {
        public CultureInfo CultureInfo { get; set; }

        public Stream TextToAudio(string text)
        {
            var synth = new SpeechSynthesizer();
            MemoryStream memoryStream = new MemoryStream();
            synth.SetOutputToWaveStream(memoryStream);
            synth.SelectVoiceByHints(VoiceGender.Female, VoiceAge.NotSet, 0, CultureInfo ?? CultureInfo.CurrentCulture);
            synth.Speak(text);

            // Rewind memorystream
            memoryStream.Flush();
            memoryStream.Seek(0, SeekOrigin.Begin);

            // Read it
            MemoryStream mp3Stream = new MemoryStream();
            using (var waveFileReader = new WaveFileReader(memoryStream))
            {
                using (var mp3Writer = new LameMP3FileWriter(mp3Stream, waveFileReader.WaveFormat, 128))
                {
                    waveFileReader.CopyTo(mp3Writer);
                }
            }

            mp3Stream.Flush();
            mp3Stream.Seek(0, SeekOrigin.Begin);
            
            return mp3Stream;
        }
    }
}
