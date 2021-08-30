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
