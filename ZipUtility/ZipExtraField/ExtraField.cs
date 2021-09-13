using ICSharpCode.SharpZipLib.Zip;
using System;

namespace ZipUtility.ZipExtraField
{
    public abstract class ExtraField
        : IExtraField, ITaggedData
    {
        private UInt16 _extraFieldId;

        protected ExtraField(UInt16 extraFieldId)
        {
            _extraFieldId = extraFieldId;
        }

        UInt16 IExtraField.ExtraFieldId => _extraFieldId;

        public abstract byte[] GetData(ZipEntryHeaderType headerType);

        public abstract void SetData(ZipEntryHeaderType headerType, byte[] data, int offset, int count);

        protected Exception GetBadFormatException(ZipEntryHeaderType headerType, byte[] data, int offset, int count)
        {
            return new BadZipFileFormatException(string.Format("Bad extra field: header={0}, type=0x{1:x4}, data=\"{2}\"", headerType, _extraFieldId, BitConverter.ToString(data, offset, count)));
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

        byte[] ITaggedData.GetData()
        {
            // ローカルファイルヘッダに書き込むときに呼び出される
            return GetData(ZipEntryHeaderType.LocalFileHeader);
        }

        void ITaggedData.SetData(byte[] data, int offset, int count)
        {
            // セントラルディレクトリヘッダから読み込むときに呼び出される
            SetData(ZipEntryHeaderType.CentralDirectoryHeader, data, offset, count);
        }
    }
}
