using System;

namespace Restfar.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class PathAttribute : Attribute
    {
        public string Value { get; set; }

        public PathAttribute(string value = "")
        {
            Value = value;
        }

    }
}
