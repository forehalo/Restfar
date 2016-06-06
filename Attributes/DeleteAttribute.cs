using System;

namespace Restfar.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class DeleteAttribute : Attribute
    {
        public DeleteAttribute(string value = "")
        {
            Value = value;
        }

        public string Value { get; set; }
    }
}
