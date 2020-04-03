using System;

namespace Lucky.Home.Protocol
{
    /// <summary>
    /// Raised when a TCP node cannot be acquired after the timeout (30 seconds).
    /// Causes the server application to terminate.
    /// </summary>
    public class DeadlockException : ApplicationException
    {
        public DeadlockException(string message)
            :base(message)
        {  }
    }
}