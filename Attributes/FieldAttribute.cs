using System;

namespace Restfar.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FieldAttribute : Attribute
    {
        public FieldAttribute(string value = "")
        {
            Value = value;
        }

        public string Value { get; set; }
    }
}
