using ICSharpCode.SharpZipLib.Zip;
using System;
using Utility;

namespace ZipUtility.ZipExtraField
{
    public abstract class ExtraField
        : IExtraField, ITaggedData
    {
        private readonly UInt16 _extraFieldId;

        protected ExtraField(UInt16 extraFieldId)
        {
            _extraFieldId = extraFieldId;
        }

        UInt16 IExtraField.ExtraFieldId => _extraFieldId;

        public abstract ReadOnlyMemory<byte>? GetData(ZipEntryHeaderType headerType);

        public abstract void SetData(ZipEntryHeaderType headerType, ReadOnlyMemory<byte> data);

        protected Exception GetBadFormatException(ZipEntryHeaderType headerType, ReadOnlyMemory<byte> data)
        {
            return new BadZipFileFormatException(string.Format("Bad extra field: header={0}, type=0x{1:x4}, data=\"{2}\"", headerType, _extraFieldId, data.ToFriendlyString()));
        }

        Int16 ITaggedData.TagID
        {
            get
            {
                unchecked
                {
                    return (Int16)_extraFieldId;
                }
            }
        }

        byte[]? ITaggedData.GetData()
        {
            // ローカルファイルヘッダに書き込むときに呼び出される
            var buffer = GetData(ZipEntryHeaderType.LocalFileHeader);
            if (!buffer.HasValue)
                return null;

            return buffer.Value.ToArray();
        }

        void ITaggedData.SetData(byte[]? data, Int32 offset, Int32 count)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            // セントラルディレクトリヘッダから読み込むときに呼び出される
            SetData(ZipEntryHeaderType.CentralDirectoryHeader, data.Slice(offset, count));
        }
    }
}
