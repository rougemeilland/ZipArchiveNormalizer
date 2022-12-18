using System;
using System.Linq;
using System.Text;
using Utility;
using Utility.IO;

namespace ZipUtility.ZipExtraField
{
    public class XceedUnicodeExtraField
        : ExtraField
    {
        private static readonly Encoding _unicodeEncoding;
        private static readonly ReadOnlyMemory<byte> _signature;

        static XceedUnicodeExtraField()
        {
            _unicodeEncoding = Encoding.Unicode;
            _signature = new byte[] { 0x4e, 0x55, 0x43, 0x58 }.AsReadOnly();
        }

        public XceedUnicodeExtraField()
            : base(ExtraFieldId)
        {

        }

        public const ushort ExtraFieldId = 0x554e;

        public override ReadOnlyMemory<byte>? GetData(ZipEntryHeaderType headerType)
        {
            if (string.IsNullOrEmpty(FullName) && string.IsNullOrEmpty(Comment))
                return null;
            var writer = new ByteArrayRenderer();
            writer.WriteBytes(_signature);
            writer.WriteUInt16LE((UInt16)(FullName?.Length ?? 0));
            writer.WriteUInt16LE((UInt16)(Comment?.Length ?? 0));
            writer.WriteBytes(_unicodeEncoding.GetBytes(FullName ?? ""));
            writer.WriteBytes(_unicodeEncoding.GetBytes(Comment ?? ""));
            return writer.ToByteArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, ReadOnlyMemory<byte> data)
        {
            FullName = null;
            Comment = null;
            var reader = new ByteArrayParser(data);
            var success = false;
            try
            {
                var signature = reader.ReadBytes(4);
                if (!signature.SequenceEqual(_signature.Span))
                    return;
                switch (headerType)
                {
                    case ZipEntryHeaderType.LocalFileHeader:
                        {
                            var fullNameCount = reader.ReadUInt16LE();
                            var fullNameBytes = reader.ReadBytes((UInt16)(fullNameCount * 2));
                            FullName = _unicodeEncoding.GetString(fullNameBytes);
                            Comment = "";
                        }
                        break;
                    case ZipEntryHeaderType.CentralDirectoryHeader:
                        {
                            var fullNameCount = reader.ReadUInt16LE();
                            var commentCount = reader.ReadUInt16LE();
                            var fullNameBytes = reader.ReadBytes((UInt16)(fullNameCount * 2));
                            var commentBytes = reader.ReadBytes((UInt16)(commentCount * 2));
                            FullName = _unicodeEncoding.GetString(fullNameBytes);
                            Comment = _unicodeEncoding.GetString(commentBytes);
                        }
                        break;
                    case ZipEntryHeaderType.Unknown:
                    default:
                        throw new ArgumentException($"Unexpected {nameof(ZipEntryHeaderType)} value", nameof(headerType));
                }
                if (reader.ReadAllBytes().Length > 0)
                    throw GetBadFormatException(headerType, data);
                success = true;
            }
            catch (UnexpectedEndOfStreamException)
            {
                throw GetBadFormatException(headerType, data);
            }
            finally
            {
                if (!success)
                {
                    FullName = null;
                    Comment = null;
                }
            }
        }

        public string? FullName { get; set; }
        public string? Comment { get; set; }
    }
}
