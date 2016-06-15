using System;

namespace Restfar.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Parameter)]
    public class HeadersAttribute : Attribute
    {
        public HeadersAttribute() { }

        public HeadersAttribute(string[] value)
        {
            Value = value;
        }
        public string[] Value { get; set; } = { };
    }
}
