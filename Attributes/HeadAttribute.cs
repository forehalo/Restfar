using System;

namespace Restfar.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    class HeadAttribute : Attribute
    {
        public HeadAttribute(string value)
        {
            Value = value;
        }
        public string Value { get; set; }
    }
}
