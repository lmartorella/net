using System;
using System.Collections.Generic;

namespace Lucky.Home.Services
{
    internal class GuiLoggerFactory : ILoggerFactory, IDisposable  
    {
        private MainWindow _masterLogger;
        private readonly List<Action> _delayedLogs = new List<Action>();

        public ILogger Create(string name)
        {
            return new LoggerImpl(false, name, null, this);
        }

        public ILogger Create(string name, bool verbose)
        {
            return new LoggerImpl(verbose, name, null, this);
        }

        public ILogger Create(string name, string subKey)
        {
            return new LoggerImpl(false, name, subKey, this);
        }

        public void Dispose()
        { }

        private class LoggerImpl : ILogger
        {
            private readonly bool _verbose;
            private readonly string _name;
            private readonly GuiLoggerFactory _owner;
            public string SubKey { get; set; }

            public LoggerImpl(bool verbose, string name, string subKey, GuiLoggerFactory owner)
            {
                _verbose = verbose;
                _name = name;
                _owner = owner;
                SubKey = subKey;
            }

            public void LogFormat(string type, string message, params object[] args)
            {
                if (_owner._masterLogger != null)
                {
                    _owner._masterLogger.LogFormat(_verbose, type, _name + (SubKey != null ? ("-" + SubKey) : "") + ": " + message, args);
                }
                else
                {
                    _owner._delayedLogs.Add(() => LogFormat(type, message, args));
                }
            }
        }

        public void Register(MainWindow logger)
        {
            _masterLogger = logger;
            foreach (Action action in _delayedLogs)
            {
                action();
            }
            _delayedLogs.Clear();
        }
    }
}