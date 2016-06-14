using System;

namespace Restfar.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class PartAttribute : Attribute
    {
        public PartAttribute(string value)
        {
            Value = value;
        }

        public string Value { get; set; }
    }
}
