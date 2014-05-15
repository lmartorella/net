using System;

namespace Lucky.Home.Core
{
    interface IConnection : IDisposable
    {
        //BinaryReader Reader { get; }
        //BinaryWriter Writer { get; }
        void Write<T>(T data);
        void Write(byte[] data);
        T Read<T>();
        void Flush();
    }
}
