﻿using System;

namespace ZipUtility.ZipExtraField
{
    class Zip64ExtendedInformationExtraFieldForLocalHeader
        : Zip64ExtendedInformationExtraField
    {
        public Zip64ExtendedInformationExtraFieldForLocalHeader()
            : base(ZipEntryHeaderType.LocalFileHeader)
        {
        }

        public UInt64 Size { get => InternalSize; set => InternalSize = value; }
        public UInt64 PackedSize { get => InternalPackedSize; set => InternalPackedSize = value; }

        protected override void GetData(ByteArrayRenderer writer)
        {
            writer.WriteUInt64LE(InternalSize);
            writer.WriteUInt64LE(InternalPackedSize);
        }

        protected override void SetData(ByteArrayParser reader, UInt16 totalCount)
        {
            if (totalCount < 16)
                throw new BadZipFileFormatException(string.Format("Too short zip64 extra field in local file header."));
            if (totalCount > 16)
                throw new BadZipFileFormatException(string.Format("Too Int64 zip64 extra field in local file header."));
            InternalSize = reader.ReadUInt64LE();
            InternalPackedSize = reader.ReadUInt64LE();
        }
    }
}
