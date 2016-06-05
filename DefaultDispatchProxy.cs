using System;
using System.Reflection;
namespace Restfar
{
    public class DefaultDispatchProxy : DispatchProxy
    {
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (targetMethod.DeclaringType == typeof(object))
                return targetMethod.Invoke(this, args);


            var serviceMethod = LoadServiceMethod(targetMethod);
            dynamic obj;
            if (targetMethod.ReturnType.IsConstructedGenericType)
            {
                var returnType = targetMethod.ReturnType.GetGenericArguments()[0];
                obj = Activator.CreateInstance(returnType);
            }
            else
            {
                obj = new object();
            }
            return serviceMethod.Call(obj, args);
        }

        private static ServiceMethod LoadServiceMethod(MethodInfo targetMethod)
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
