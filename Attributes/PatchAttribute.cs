using System;

namespace Restfar.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PatchAttribute : Attribute
    {
        public PatchAttribute(string value = "")
        {
            Value = value;
        }

        public string Value { get; set; }
    }
}
