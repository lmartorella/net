using System;

namespace Lucky.Home.Application
{
    public class AppBase : IDisposable
    {
        internal protected virtual void OnInitialize()
        {
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool dispose)
        {
        }
    }
}
