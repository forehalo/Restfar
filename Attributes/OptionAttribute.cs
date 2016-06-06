using System;

namespace Restfar.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class OptionAttribute : Attribute
    {

        public OptionAttribute(string value = "")
        {
            Value = value;
        }

        public string Value { get; set; }
    }
}
