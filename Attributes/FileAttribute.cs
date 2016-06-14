using System;

namespace Restfar.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FileAttribute : Attribute
    {
        public FileAttribute(string value)
        {
            Value = value;
        }

        public string Value { get; set; }
    }
}
