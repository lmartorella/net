using System.IO.Ports;

namespace SerialEcho
{
    class Program
    {
        static void Main(string[] args)
        {
            var port = new SerialPort(args[0], int.Parse(args[1]), Parity.None, 8, StopBits.One);
            port.Open();
            new FakeSamil(port).Run();
        }
    }
}
