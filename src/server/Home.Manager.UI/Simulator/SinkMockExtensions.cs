﻿using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Lucky.Home.Simulator
{
    public static class SinkMockExtensions
    {
        public static void WriteTwocc(this BinaryWriter writer, string code)
        {
            var bytes = Encoding.ASCII.GetBytes(code);
            if (bytes.Length != 2)
            {
                throw new ArgumentException("code");
            }
            writer.Write(bytes, 0, 2);
        }

        public static void WriteFourcc(this BinaryWriter writer, string code)
        {
            var bytes = Encoding.ASCII.GetBytes(code);
            if (bytes.Length != 4)
            {
                throw new ArgumentException("code");
            }
            writer.Write(bytes, 0, 4);
        }

        public static void WriteString(this BinaryWriter writer, string str)
        {
            var buffer = Encoding.ASCII.GetBytes(str);
            writer.Write((ushort)buffer.Length);
            writer.Write(buffer);
        }

        public static string GetFourCc(this ISinkMock sinkMock)
        {
            return sinkMock.GetType().GetCustomAttribute<MockSinkAttribute>().FourCc;
        }
    }
}