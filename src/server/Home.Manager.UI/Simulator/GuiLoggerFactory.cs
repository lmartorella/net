using System;
using System.Collections.Generic;
using Lucky.Home.Services;
using Lucky.Home.Views;

namespace Lucky.HomeMock
{
    internal class NodeLoggerFactory : ILoggerFactory, IDisposable  
    {
        private MasterNodeView _masterLogger;
        private readonly List<Action> _delayedLogs = new List<Action>();

        public ILogger Create(string name, bool verbose = false)
        {
            return new LoggerImpl(verbose, name, this);
        }

        public void Dispose()
        { }

        private class LoggerImpl : ILogger
        {
            private readonly bool _verbose;
            private readonly string _name;
            private readonly NodeLoggerFactory _owner;

            public LoggerImpl(bool verbose, string name, NodeLoggerFactory owner)
            {
                _verbose = verbose;
                _name = name;
                _owner = owner;
            }

            public void LogFormat(string type, string message, params object[] args)
            {
                if (_owner._masterLogger != null)
                {
                    _owner._masterLogger.LogFormat(_verbose, type, _name + ": " + message, args);
                }
                else
                {
                    _owner._delayedLogs.Add(() => LogFormat(type, message, args));
                }
            }
        }

        public void Register(MasterNodeView logger)
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