using Lucky.Charting;
using Lucky.Home.Notification;
using Lucky.Services;
using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading;

namespace Lucky
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Manager.Register<LoggerFactory, ILoggerFactory>();

            //SendTextMail();
            SendHtmlMail();

            WaitBreak();
        }

        private static void SendTextMail()
        {
            var notificationSvc = Manager.GetService<INotificationService>();
            notificationSvc.SendMail("Test Mail", "Ignore this message");
        }

        private static void SendHtmlMail()
        {
            var notificationSvc = Manager.GetService<INotificationService>();
            var body = @"<html>
                          <body>
                            <table width=""100%"">
                            <tr>
                                <td style=""font-style:arial; color:maroon; font-weight:bold"">
                                Test HTML message <br>
                                <img src=""cid:chart"">
                                </td>
                            </tr>
                            </table>
                            </body>
                            </html>";
            var attachments = new Tuple<Stream, ContentType, string>[]
            {
                Tuple.Create(CreateChartSvg(), new ContentType("image/png"), "chart")
            };
            notificationSvc.SendHtmlMail("Test HTML Mail", body, attachments);
        }

        private static Stream CreateChartSvg()
        {
            var chart = new CategoryChart("Title", "X Axis", "Y Axis");
            var values = new[] { 12.0, 24.0, 48.0, 56.0 };
            var names = new[] { "One", "Two", "Three", "Four" };
            chart.AddSerie(values.Zip(names, (i1, i2) => Tuple.Create(i1, i2)));

            return chart.ToPng();
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
