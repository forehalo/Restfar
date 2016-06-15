using System;

namespace Restfar.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
    public class SuccessAttribute : Attribute
    {
    }
}
