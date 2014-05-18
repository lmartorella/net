using System;

namespace Lucky.Home.Core
{
    interface IConnection : IDisposable
    {
        void Write<T>(T data);
        T Read<T>();
    }
}
