using System;
using System.Runtime.Serialization;

namespace ZipUtility
{
    [Serializable]
    public class EncryptedZipFileNotSupportedException
        : NotSupportedSpecificationException
    {
        public EncryptedZipFileNotSupportedException(string required)
            : base(string.Format("Encrypted ZIP file is not supported.: required-function=\"{0}\"", required))
        {
            Required = required;
        }

        public EncryptedZipFileNotSupportedException(string message, string entryName, string required)
            : base(message)
        {
            Required = required;
        }

        public EncryptedZipFileNotSupportedException(string message, Exception inner, string entryName, string required)
            : base(message, inner)
        {
            Required = required;
        }

        protected EncryptedZipFileNotSupportedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Required = info.GetString("Required");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Required", Required);
        }

        public string Required { get; }
    }
}
