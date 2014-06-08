using System;
using System.Linq;

namespace RamTestPetterns
{
    class Program
    {
        private class RamChip
        {
            private readonly byte[] _ram;
            
            public RamChip(int size)
            {
                _ram = new byte[size];
            }

            public int Size
            {
                get
                {
                    return _ram.Length;
                }
            }

            public void Write(byte[] buffer, int addr, int bufSize)
            {
                for (int i = 0; i < bufSize; i++, addr++)
                {
                    _ram[addr % _ram.Length] = buffer[i];
                }
            }

            public void Read(byte[] buffer, int addr, int bufSize)
            {
                for (int i = 0; i < bufSize; i++, addr++)
                {
                    buffer[i] = _ram[addr % _ram.Length];
                }
            }
        }

        private class RamBank
        {
            private readonly int _chipSize;
            private readonly RamChip[] _chips;

            public RamBank(int banks, int chipSize)
            {
                _chipSize = chipSize;
                _chips = Enumerable.Range(0, banks).Select(i => new RamChip(chipSize)).ToArray();
            }

            public int Size
            {
                get { return _chips.Sum(c => c.Size); }
            }

            public void Write(byte[] buffer, int addr, int bufSize)
            {
                _chips[addr / _chipSize].Write(buffer, addr % _chipSize, bufSize);
            }

            public void Read(byte[] buffer, int addr, int bufSize)
            {
                _chips[addr / _chipSize].Read(buffer, addr % _chipSize, bufSize);
            }
        }

        class SumGenerator : ISerieGenerator
        {
            private readonly byte _seed;
            private byte _next;

            public SumGenerator(byte seed)
            {
                _seed = seed;
            }

            public void Reset()
            {
                _next = 0;
            }

            public byte Next()
            {
                _next += _seed;
                return _next;
            }
        }


        class RandomGenerator : ISerieGenerator
        {
            private readonly int _seed;
            private Random _random;

            public RandomGenerator(int seed)
            {
                _seed = seed;
                Reset();
            }

            public void Reset()
            {
                _random = new Random(_seed);
            }

            public byte Next()
            {
                return (byte)_random.Next(0, 256);
            }
        }


        static void Main()
        {
            RamBank bank = new RamBank(4, 32 * 1024);
            try
            {
                Test(bank, new SumGenerator(251), 32);
                //Test(bank, new RandomGenerator(251), 1500);
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine(exc.Message);
            }

            Console.WriteLine("OK");
        }

        private interface ISerieGenerator
        {
            void Reset();
            byte Next();
        }

        private static void Test(RamBank bank, ISerieGenerator generator, int bufSize)
        {
            generator.Reset();
            int size = bank.Size;

            byte[] buffer = new byte[bufSize];
            for (int addr = 0; addr < size; addr += bufSize)
            {
                for (int j = 0; j < bufSize; j++)
                {
                    buffer[j] = generator.Next();
                }

                bank.Write(buffer, addr, bufSize);
            }

            generator.Reset();
            for (int addr = 0; addr < size; addr += bufSize)
            {
                bank.Read(buffer, addr, bufSize);

                for (int j = 0; j < bufSize; j++)
                {
                    byte b = generator.Next();
                    if (buffer[j] != b)
                    {
                        throw new ApplicationException(string.Format("Error at Addr: 0x{0:X5}, Expected 0x{1:X2} but was 0x{2:X2}",
                            addr + j, b, buffer[j]));
                    }
                }
            }
        }
    }
}
