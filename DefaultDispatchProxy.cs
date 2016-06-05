using System;
using System.Reflection;
namespace Restfar
{
    /// <summary>
    /// Dispatch proxy used to dispatch call of unimplemented interface method.
    /// </summary>
    public class DefaultDispatchProxy : DispatchProxy
    {
        /// <summary>
        /// Invoke the called method
        /// </summary>
        /// <param name="targetMethod"></param>
        /// <param name="args"></param>
        /// <returns>the required result</returns>
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (targetMethod.DeclaringType == typeof(object))
                return targetMethod.Invoke(this, args);

            var serviceMethod = RestfarClient.LoadServiceMethod(targetMethod);
            // Dynamic `Call` function return type.
            // If return type of targetMethod is not generic(Task only), pass object as generic type
            // If generic return type(Task<T>), pass the instance of T as generic type.
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
    }
}
