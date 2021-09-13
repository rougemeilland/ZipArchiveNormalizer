using System;
using System.Linq;
using System.Text;
using ZipUtility.Helper;

namespace ZipUtility.ZipExtraField
{
    public class XceedUnicodeExtraField
        : ExtraField
    {
        private static Encoding _unicodeEncoding;
        private static byte[] _signature;

        static XceedUnicodeExtraField()
        {
            _unicodeEncoding = Encoding.Unicode;
            _signature = new byte[] { 0x4e, 0x55, 0x43, 0x58 };
        }

        public XceedUnicodeExtraField()
            : base(ExtraFieldId)
        {

        }

        public const ushort ExtraFieldId = 0x554e;

        public override byte[] GetData(ZipEntryHeaderType headerType)
        {
            if (string.IsNullOrEmpty(FullName) && string.IsNullOrEmpty(Comment))
                return null;
            var writer = new ByteArrayOutputStream();
            writer.WriteBytes(_signature);
            writer.WriteUInt16LE((UInt16)(FullName?.Length ?? 0));
            writer.WriteUInt16LE((UInt16)(Comment?.Length ?? 0));
            writer.WriteBytes(_unicodeEncoding.GetBytes(FullName ?? ""));
            writer.WriteBytes(_unicodeEncoding.GetBytes(Comment ?? ""));
            return writer.ToByteSequence().ToArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, byte[] data, int index, int count)
        {
            FullName = null;
            Comment = null;
            var reader = new ByteArrayInputStream(data, index, count);
            var success = false;
            try
            {
                var signature = reader.ReadBytes(4);
                if (!signature.SequenceEqual(_signature))
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
                        throw new ArgumentException();
                }
                if (reader.ReadToEnd().Length > 0)
                    throw GetBadFormatException(headerType, data, index, count);
                success = true;
            }
            catch (UnexpectedEndOfStreamException)
            {
                throw GetBadFormatException(headerType, data, index, count);
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

        public string FullName { get; set; }
        public string Comment { get; set; }
        public int CodePage => _unicodeEncoding.CodePage;
    }
}
