using System;

namespace Restfar.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PostAttribute : Attribute
    {
        public PostAttribute(string value = "")
        {
            Value = value;
        }

        public string Value { set; get; }
    }
}
