using System;
using System.Reflection;
namespace Restfar
{
    /// <summary>
    /// Create service interface caller use reflection and config the client.
    /// </summary>
    public class RestfarClient
    {
        public static T Create<T>()
        {
            return DispatchProxy.Create<T, DefaultDispatchProxy>();
        }

        public static ServiceMethod LoadServiceMethod(MethodInfo targetMethod)
        {
            ServiceMethod result = null;
            //TODO Cache
            if (result == null)
            {
                result = new ServiceMethod(targetMethod);
            }
            return result;
        }
    }
}
