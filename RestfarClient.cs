using System;
using System.Reflection;
namespace Restfar
{
    /// <summary>
    /// Create service interface caller use reflection and config the client.
    /// </summary>
    public class RestfarClient
    {
        /// <summary>
        /// Base uri of API service.
        /// eg. "https://api.github.com/"
        /// </summary>
        public static string BaseUri { get; set; }

        /// <summary>
        /// Conscructor
        /// </summary>
        /// <param name="baseUri"></param>
        public RestfarClient(string baseUri)
        {
            BaseUri = baseUri.TrimEnd('/') + "/";
        }

        /// <summary>
        /// Generate instance of given interface using DispatchProxy
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Create<T>()
        {
            return DispatchProxy.Create<T, DefaultDispatchProxy>();
        }

        /// <summary>
        /// Load a method caller proxy from target method.
        /// process all in ServiceMethod class.
        /// </summary>
        /// <param name="targetMethod"></param>
        /// <returns></returns>
        public static ServiceMethod LoadServiceMethod(MethodInfo targetMethod)
        {
            ServiceMethod result = null;
            //TODO Cache
            if (result == null)
            {
                result = new ServiceMethod(targetMethod, BaseUri);
            }
            return result;
        }
    }
}
