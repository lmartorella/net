using System;
using System.IO;

namespace Lucky.Home.Core
{
    interface IConnection : IDisposable
    {
        BinaryReader Reader { get; }
        BinaryWriter Writer { get; }
    }
}
