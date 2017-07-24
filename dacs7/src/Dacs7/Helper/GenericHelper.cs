using System;
using System.Linq;
using System.Reflection;

namespace Dacs7.Helper
{
    internal class GenericHelper
    {
        //TODO create cache

        public static object InvokeGenericMethod<T>(Type genericType, string methodName , object[] parameters)
        {
            var method = typeof(T).GetMethod(methodName, parameters.Select(x => x.GetType()).ToArray());
            var genericMethod = method.MakeGenericMethod(genericType);
            return genericMethod.Invoke(null, parameters);
        }
    }
}
