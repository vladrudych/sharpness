using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sharpness.Build
{
    public static class BuildServices
    {
        static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public static T Resolve<T>() where T : class
        {
            return Resolve(typeof(T)) as T;
        }

        static object Resolve(Type type)
        {
            if (!_services.ContainsKey(type))
            {
                var constructor = type
                    .GetConstructors(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault();

                if (constructor == null)
                {
                    throw new Exception($"There is no public constructor for type {type.AssemblyQualifiedName}!");
                }

                var parameters = constructor.GetParameters();
                var arguments = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    arguments[i] = Resolve(parameters[i].ParameterType);
                }

                _services[type] = Activator.CreateInstance(type, arguments);
            }

            return _services[type];
        }
    }
}
