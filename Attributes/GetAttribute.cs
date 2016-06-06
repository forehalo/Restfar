using System;

namespace Restfar.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class  GetAttribute : Attribute
    {
        public string Value { get; set; }

        public GetAttribute(string value = "")
        {
            Value = value;
        }
    }
}
