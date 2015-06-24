﻿using System;

namespace Lucky.Home.Core
{
    interface ILogger : IService
    {
        void Log(string message);
        void Log(string message, string param1, object value1);
        void Log(string message, string param1, object value1, string param2, object value2);
        void Warning(string message);
        void Warning(string message, string param1, object value1);
        void Warning(string message, string param1, object value1, string param2, object value2);
        void Exception(Exception exc);
    }
}
