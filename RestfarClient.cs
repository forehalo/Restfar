using System;
using System.Reflection;
namespace Restfar
{
    public class RestfarClient
    {
        public static T Create<T>()
        {
            return DispatchProxy.Create<T, DefaultDispatchProxy>();
        }
    }
}
