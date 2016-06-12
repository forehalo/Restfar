using System;

namespace Restfar.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class QueryAttribute : Attribute
    {
        public QueryAttribute(string value)
        {
            Value = value;
        }

        public string Value { get; set; }
    }
}
