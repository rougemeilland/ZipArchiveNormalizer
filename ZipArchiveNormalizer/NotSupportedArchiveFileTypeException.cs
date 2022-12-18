using System;
using System.Runtime.Serialization;

namespace ZipArchiveNormalizer
{
    [Serializable]
    public class NotSupportedArchiveFileTypeException
        : Exception
    {
        public NotSupportedArchiveFileTypeException(string filePath)
            : base($"Unknown archive file type: path=\"{filePath}\"")
        {
            FilePath = filePath;
        }

        public NotSupportedArchiveFileTypeException(string message, string filePath)
            : base(message)
        {
            FilePath = filePath;
        }

        public NotSupportedArchiveFileTypeException(string message, string filePath, Exception inner)
            : base(message, inner)
        {
            FilePath = filePath;
        }

        protected NotSupportedArchiveFileTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            FilePath = info.GetString(nameof(FilePath)) ?? "???";
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(FilePath), FilePath);
        }

        public string FilePath { get; }
    }
}
