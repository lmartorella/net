using System;
using System.Collections.Generic;

namespace Lucky.Home.Core
{
    public static class Manager
    {
        private static readonly Dictionary<Type, Type> s_types = new Dictionary<Type, Type>();
        private static readonly Dictionary<Type, object> s_instances = new Dictionary<Type, object>();

        public static T GetService<T>() where T : IService
        {
            object retValue;
            if (!s_instances.TryGetValue(typeof(T), out retValue))
            {
                Type type = GetType<T>();
                retValue = Activator.CreateInstance(type);
                s_instances[typeof(T)] = retValue;
            }
            return (T)retValue;
        }

        private static Type GetType<T>() where T : IService
        {
            Type implType;
            if (!s_types.TryGetValue(typeof (T), out implType))
            {
                implType = typeof (T);
                s_types.Add(implType, implType);
            }
            return implType;
        }

        public static void Register<TC, TI>() where TI : IService 
                                              where TC : class, TI, new()
        {
            s_types[typeof(TI)] = typeof(TC);
        }
    }
}
