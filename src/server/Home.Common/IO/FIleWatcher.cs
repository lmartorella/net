using System;
using System.IO;
using System.Threading;

namespace Lucky.Home.IO
{
    /// <summary>
    /// Wrapper around <see cref="FileSystemWatcher"/>, with debounce
    /// </summary>
    public class FileWatcher : IDisposable
    {
        private Timer _debounceTimer;
        private FileSystemWatcher _cfgFileObserver;

        public FileWatcher(FileInfo fileInfo)
        {
            _cfgFileObserver = new FileSystemWatcher(fileInfo.DirectoryName, fileInfo.Name);
            _cfgFileObserver.Changed += (o, e) => Debounce(() => Changed?.Invoke(this, EventArgs.Empty));
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
    }
}
