using System;
using System.IO.Ports;
using System.Text;

namespace SerialEcho
{
    class Program
    {
        static void Main(string[] args)
        {
            var port = new SerialPort(args[0], 9600, Parity.None, 8, StopBits.One);
            port.ReceivedBytesThreshold = 1;
            port.Encoding = Encoding.ASCII;
            port.NewLine = "$";
            port.Open();
            port.WriteTimeout = SerialPort.InfiniteTimeout;

            do
            {
                var rx = port.ReadLine();
                var tx = rx.ToUpper();
                port.WriteLine(tx);
                Console.WriteLine("RX  <- {0} ({1}+1 chars)", rx, rx.Length);
                Console.WriteLine(" TX <- " + tx);
            } while (true);
        }
    }
}
