using System;
using System.Linq;
using ZipUtility.Helper;

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

        public override byte[] GetData(ZipEntryHeaderType headerType)
        {
            var ok = false;
            var writer = new ByteArrayOutputStream();

            writer.WriteUInt32LE(0); //Reserved

            var dataOfSubTag0001 = GetDataForSubTag0001();
            if (dataOfSubTag0001 != null)
            {
                writer.WriteUInt16LE(0x0001);
                writer.WriteUInt16LE((UInt16)dataOfSubTag0001.Length);
                writer.WriteBytes(dataOfSubTag0001);
                ok = true;
            }

            return ok == true ? writer.ToByteSequence().ToArray() : null;
        }

        public override void SetData(ZipEntryHeaderType headerType, byte[] data, int index, int count)
        {
            LastWriteTimeUtc = null;
            LastAccessTimeUtc = null;
            CreationTimeUtc = null;

            var reader = new ByteArrayInputStream(data, index, count);
            reader.ReadUInt32LE(); //Reserved

            while (!reader.IsEndOfStream())
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
        }

        private byte[] GetDataForSubTag0001()
        {
            // 最終更新日時/最終アクセス日時/作成日時のいずれかが未設定の場合は、この拡張フィールドは無効とする。
            if (LastWriteTimeUtc == null ||
                LastAccessTimeUtc == null ||
                CreationTimeUtc == null)
            {
                return null;
            }
            var writer = new ByteArrayOutputStream();
            writer.WriteUInt64LE((UInt64)LastWriteTimeUtc.Value.ToFileTimeUtc());
            writer.WriteUInt64LE((UInt64)LastAccessTimeUtc.Value.ToFileTimeUtc());
            writer.WriteUInt64LE((UInt64)CreationTimeUtc.Value.ToFileTimeUtc());
            return writer.ToByteSequence().ToArray();
        }

        private void SetDataForSubTag0001(byte[] data)
        {
            var reader = new ByteArrayInputStream(data, 0, data.Length);
            LastWriteTimeUtc = DateTime.FromFileTimeUtc((Int64)reader.ReadUInt64LE());
            LastAccessTimeUtc = DateTime.FromFileTimeUtc((Int64)reader.ReadUInt64LE());
            CreationTimeUtc = DateTime.FromFileTimeUtc((Int64)reader.ReadUInt64LE());
        }
    }
}