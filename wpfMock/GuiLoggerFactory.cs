using System;
using System.Collections.Generic;
using Lucky.Home.Core;

namespace Lucky.HomeMock
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class GuiLoggerFactory : ServiceBase, ILoggerFactory
    {
        private ILogger _masterLogger;
        private List<Action> _delayedLogs = new List<Action>();

        public GuiLoggerFactory() 
            :base(null)
        { }

        public ILogger Create(string name)
        {
            return new Logger(name, this);
        }

        private class Logger : ILogger
        {
            private readonly string _name;
            private readonly GuiLoggerFactory _owner;

            public Logger(string name, GuiLoggerFactory owner)
            {
                _name = name;
                _owner = owner;
            }

            public void LogFormat(string type, string message, params object[] args)
            {
                if (_owner._masterLogger != null)
                {
                    _owner._masterLogger.LogFormat(type, _name + ": " + message, args);
                }
                else
                {
                    _owner._delayedLogs.Add(() => LogFormat(type, message, args));
                }
            }
        }

        public void Register(ILogger logger)
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