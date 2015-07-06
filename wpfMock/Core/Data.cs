using System;

namespace Lucky.HomeMock.Core
{
    static class Data
    {
        public static readonly object LockObject = new object();
        public static Guid DeviceId { get; set; }
    }
}
