using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucky.Home.Core
{
    static class Manager
    {
        private static Dictionary<Type, Type> s_types = new Dictionary<Type, Type>();
        private static Dictionary<Type, object> s_instances = new Dictionary<Type, object>();

        public static T GetService<T>() where T : IService
        {
            object retValue;
            if (!s_instances.TryGetValue(typeof(T), out retValue))
            {
                retValue = Activator.CreateInstance(s_types[typeof(T)]);
                s_instances[typeof(T)] = retValue;
            }
            return (T)retValue;
        }

        public static void Register<TC, TI>() where TI : IService 
                                              where TC : class, TI, new()
        {
            s_types[typeof(TI)] = typeof(TC);
        }
    }
}
