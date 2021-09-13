using System;
using ZipUtility.Helper;

namespace ZipUtility.ZipExtraField
{
    class Zip64ExtendedInformationExtraFieldForLocalHeader
        : Zip64ExtendedInformationExtraField
    {
        public Zip64ExtendedInformationExtraFieldForLocalHeader()
            : base(ZipEntryHeaderType.LocalFileHeader)
        {
        }

        public Int64 Size { get => InternalSize; set => InternalSize = value; }
        public Int64 PackedSize { get => InternalPackedSize; set => InternalPackedSize = value; }

        protected override void GetData(ByteArrayOutputStream writer)
        {
            writer.WriteInt64LE(InternalSize);
            writer.WriteInt64LE(InternalPackedSize);
        }

        protected override void SetData(ByteArrayInputStream reader, UInt16 totalCount)
        {
            if (totalCount < 16)
                throw new BadZipFileFormatException(string.Format("Too short zip64 extra field in local file header."));
            if (totalCount > 16)
                throw new BadZipFileFormatException(string.Format("Too long zip64 extra field in local file header."));
            InternalSize = reader.ReadInt64LE();
            InternalPackedSize = reader.ReadInt64LE();
        }
    }
}