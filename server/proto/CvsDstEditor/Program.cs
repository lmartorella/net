using System;
using System.IO;

namespace CvsDstEditor
{
    class Program
    {
        private const string DAY_TS_FORMAT = "hh\\:mm\\:ss";

        //static void Main(string[] args)
        //{
        //    int hourDelta = int.Parse(args[0]);
        //    ProcessCsv(Console.OpenStandardInput(), Console.OpenStandardOutput(), hourDelta);
        //}

        static void Main(string[] args)
        {
            int hourDelta = int.Parse(args[0]);
            DirectoryInfo din = new DirectoryInfo(args[1]);
            DirectoryInfo dout = new DirectoryInfo(args[2]);
            foreach (var file in din.GetFiles())
            {
                FileInfo filein = new FileInfo(Path.Combine(din.FullName, file.Name));
                FileInfo fileout = new FileInfo(Path.Combine(dout.FullName, file.Name));
                ProcessCsv(filein.OpenRead(), fileout.OpenWrite(), hourDelta);
            }
        }

        /// <summary>
        /// Read first field of each CSV. If a timestamp, change the hour
        /// </summary>
        static void ProcessCsv(Stream input, Stream output, int hourDelta)
        {
            using (var reader = new StreamReader(input))
            {
                using (var writer = new StreamWriter(output))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var parts = line.Split(',');

                        TimeSpan ts;
                        if (TimeSpan.TryParseExact(parts[0], DAY_TS_FORMAT, null, out ts))
                        {
                            ts = ts.Add(TimeSpan.FromHours(hourDelta));
                            parts[0] = string.Format("{0:" + DAY_TS_FORMAT + "}", ts);
                        }

                        writer.WriteLine(string.Join(",", parts));
                    }
                }
            }
        }
    }
}
