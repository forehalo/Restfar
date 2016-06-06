using System;

namespace Restfar.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string field, string value)
        {
            Field = field;
            Value = value;
        }

        public string Field { get; set; }
        public string Value { get; set; }
    }
}
