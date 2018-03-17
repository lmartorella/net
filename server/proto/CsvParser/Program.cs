using Lucky;
using Lucky.Home.Db;
using Lucky.Home.Power;
using System;
using System.IO;

namespace CsvParser
{
    class Program
    {
        static void Main(string[] args)
        {
            DirectoryInfo folder = new DirectoryInfo(args[0]);
            FsTimeSeries<PowerData, DayPowerData>.AggregateData(new LoggerFactory.ConsoleLogger("Console"), folder, DateTime.Now);
        }
    }
}

