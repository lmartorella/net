using System;
using System.Collections.Generic;
using Lucky.Services;

namespace Lucky.HomeMock
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class GuiLoggerFactory : ServiceBase, ILoggerFactory
    {
        private MainWindow _masterLogger;
        private readonly List<Action> _delayedLogs = new List<Action>();

        public ILogger Create(string name, bool verbose = false)
        {
            return new LoggerImpl(verbose, name, this);
        }

        private class LoggerImpl : ILogger
        {
            private readonly bool _verbose;
            private readonly string _name;
            private readonly GuiLoggerFactory _owner;

            public LoggerImpl(bool verbose, string name, GuiLoggerFactory owner)
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