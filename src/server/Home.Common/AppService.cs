using System.Threading.Tasks;
using Lucky.Home.Services;

namespace Lucky.Home
{
    /// <summary>
    /// Base class of application logic
    /// </summary>
    public abstract class AppService : ServiceBase
    {
        private readonly TaskCompletionSource<object> _killDefer = new TaskCompletionSource<object>();

        /// <summary>
        /// Override to implement the start logic
        /// </summary>
        /// <returns></returns>
        public abstract Task Start();

        internal async Task Run()
        {
            await _killDefer.Task;
        }

        internal void Kill(string reason)
        {
            Logger.LogStderr("Server killing: + " + reason + ". Stopping devices...");
            _killDefer.TrySetResult(null);
        }
    }
}
