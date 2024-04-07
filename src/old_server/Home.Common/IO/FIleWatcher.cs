using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lucky.Home.IO
{
    /// <summary>
    /// Wrapper around <see cref="FileSystemWatcher"/>, with debounce
    /// </summary>
    public class FileWatcher : IDisposable
    {
        private Timer _debounceTimer;
        private FileSystemWatcher _cfgFileObserver;
        private Queue<TaskCompletionSource<bool>> _waiterQueue = new Queue<TaskCompletionSource<bool>>();

        public FileWatcher(FileInfo fileInfo)
        {
            _cfgFileObserver = new FileSystemWatcher(fileInfo.DirectoryName, fileInfo.Name);
            _cfgFileObserver.Changed += (o, e) => Debounce(() =>
            {
                lock (_waiterQueue)
                {
                    // Check if suspended
                    if (_waiterQueue.Count > 0)
                    {
                        _waiterQueue.Dequeue().SetResult(true);
                        return;
                    }
                }
                // Else...
                Changed?.Invoke(this, EventArgs.Empty);
            });
            _cfgFileObserver.NotifyFilter = NotifyFilters.LastWrite;
            _cfgFileObserver.EnableRaisingEvents = true;
        }

        public event EventHandler Changed;

        /// <summary>
        /// Used to read config when the FS notifies changes
        /// </summary>
        private void Debounce(Action handler)
        {
            // Event comes two time (the first one with an empty file)
            if (_debounceTimer == null)
            {
                _debounceTimer = new Timer(o =>
                {
                    _debounceTimer = null;
                    handler();
                }, null, 1000, Timeout.Infinite);
            }
        }

        public void Dispose()
        {
            _cfgFileObserver.Dispose();
        }

        public Task SuspendAndWaitForUpdate()
        {
            lock (_waiterQueue)
            {
                var source = new TaskCompletionSource<bool>();
                _waiterQueue.Enqueue(source);
                return source.Task;
            }
        }
    }
}
