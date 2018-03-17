using Lucky.Charting;
using System;
using System.IO;
using System.Linq;

namespace ChartGen
{
    class Program
    {
        static void Main(string[] args)
        {
            var chart = new CategoryChart("Title", "X Axis", "Y Axis");
            var values = new[] { 12.0, 24.0, 48.0, 56.0 };
            var names = new[] { "One", "Two", "Three", "Four" };
            chart.AddSerie(values.Zip(names, (i1, i2) => Tuple.Create(i1, i2)));

            var file = chart.CreateImage(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "chart.svg"));
            Console.WriteLine(file.FullName);
        }
    }
}
