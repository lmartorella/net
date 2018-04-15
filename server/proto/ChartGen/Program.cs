using Lucky.Charting;
using System;
using System.IO;
using System.Linq;

namespace ChartGen
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var chart = new CategoryChart("Title", "X Axis", "Y Axis");
            var values = new[] { 12.0, 24.0, 48.0, 56.0 };
            var names = new[] { "One", "Two", "Three", "Four" };
            chart.AddSerie(values.Zip(names, (i1, i2) => Tuple.Create(i1, i2)));

            var file = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "chart.png"));
            using (var png = chart.ToPng(300, 300))
            {
                using (var stream = file.OpenWrite())
                {
                    png.CopyTo(stream);
                }
            }
            Console.WriteLine(file.FullName);
        }
    }
}
