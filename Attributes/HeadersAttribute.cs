using System;

namespace Restfar.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface)]
    public class HeadersAttribute : Attribute
    {
        public HeadersAttribute(string[] value)
        {
            Value = value;
        }
        public string[] Value { get; set; } = { };
    }
}
