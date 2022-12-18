using System;
using Utility.IO;

namespace ZipUtility.ZipExtraField
{
    public class NtfsExtraField
        : TimestampExtraField
    {
        public NtfsExtraField()
            : base(ExtraFieldId)
        {
            LastWriteTimeUtc = null;
            LastAccessTimeUtc = null;
            CreationTimeUtc = null;
        }

        public const ushort ExtraFieldId = 10;

        public override ReadOnlyMemory<byte>? GetData(ZipEntryHeaderType headerType)
        {
            var ok = false;
            var writer = new ByteArrayRenderer();

            writer.WriteUInt32LE(0); //Reserved

            var dataOfSubTag0001 = GetDataForSubTag0001();
            if (dataOfSubTag0001.HasValue)
            {
                writer.WriteUInt16LE(0x0001);
                writer.WriteUInt16LE((UInt16)dataOfSubTag0001.Value.Length);
                writer.WriteBytes(dataOfSubTag0001.Value);
                ok = true;
            }
            if (!ok)
                return null;
            return writer.ToByteArray();
        }

        public override void SetData(ZipEntryHeaderType headerType, ReadOnlyMemory<byte> data)
        {
            LastWriteTimeUtc = null;
            LastAccessTimeUtc = null;
            CreationTimeUtc = null;
            var reader = new ByteArrayParser(data);
            var success = false;
            try
            {
                reader.ReadUInt32LE(); //Reserved
                while (!reader.IsEmpty)
                {
                    var subTagId = reader.ReadUInt16LE();
                    var subTagLength = reader.ReadUInt16LE();
                    var subTagData = reader.ReadBytes(subTagLength);
                    switch (subTagId)
                    {
                        case 0x001:
                            SetDataForSubTag0001(subTagData);
                            break;
                        default:
                            // unknown sub tag id
                            break;
                    }
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
                    LastWriteTimeUtc = null;
                    LastAccessTimeUtc = null;
                    CreationTimeUtc = null;
                }
            }
        }

        private ReadOnlyMemory<byte>? GetDataForSubTag0001()
        {
            // 最終更新日時/最終アクセス日時/作成日時のいずれかが未設定の場合は、この拡張フィールドは無効とする。
            if (LastWriteTimeUtc is null ||
                LastAccessTimeUtc is null ||
                CreationTimeUtc is null)
            {
                return null;
            }
            var writer = new ByteArrayRenderer();
            writer.WriteUInt64LE((UInt64)LastWriteTimeUtc.Value.ToFileTimeUtc());
            writer.WriteUInt64LE((UInt64)LastAccessTimeUtc.Value.ToFileTimeUtc());
            writer.WriteUInt64LE((UInt64)CreationTimeUtc.Value.ToFileTimeUtc());
            return writer.ToByteArray();
        }

        private void SetDataForSubTag0001(ReadOnlyMemory<byte> data)
        {
            var reader = new ByteArrayParser(data);
            LastWriteTimeUtc = DateTime.FromFileTimeUtc((Int64)reader.ReadUInt64LE());
            LastAccessTimeUtc = DateTime.FromFileTimeUtc((Int64)reader.ReadUInt64LE());
            CreationTimeUtc = DateTime.FromFileTimeUtc((Int64)reader.ReadUInt64LE());
        }
    }
}
