using System;
using System.IO.Ports;
using System.Text;

namespace SerialEcho
{
    class Program
    {
        static void Main(string[] args)
        {
            var port = new SerialPort("COM1", 9600, Parity.None, 8, StopBits.One);
            port.ReceivedBytesThreshold = 1;
            port.Encoding = Encoding.ASCII;
            port.NewLine = " ";
            port.Open();

            do
            {
                var str = port.ReadLine();
                port.WriteLine(str.ToUpper());
                Console.WriteLine("Received: " + str);
            } while (true);
        }
    }
}
