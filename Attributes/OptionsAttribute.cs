using System;

namespace Restfar.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class OptionsAttribute : Attribute
    {

        public OptionsAttribute(string value = "")
        {
            Value = value;
        }

        public string Value { get; set; }
    }
}
