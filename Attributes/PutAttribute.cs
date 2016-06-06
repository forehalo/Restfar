using System;

namespace Restfar.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PutAttribute : Attribute
    {
        public PutAttribute(string value = "")
        {
            Value = value;
        }

        public string Value { get; set; }
    }
}
