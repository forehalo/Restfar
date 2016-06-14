using System;

namespace Restfar.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    class FileAttribute : Attribute
    {
        public FileAttribute(string filePath)
        {
            FilePath = filePath;
        }

        public string FilePath { get; set; }
    }
}
