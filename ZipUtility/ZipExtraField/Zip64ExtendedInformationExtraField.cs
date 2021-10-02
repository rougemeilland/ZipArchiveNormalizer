using System;
using System.Linq;
using Utility;
using Utility.IO;
using ZipUtility.Helper;
using ZipUtility.ZipFileHeader;

namespace ZipUtility.ZipExtraField
{
    abstract class Zip64ExtendedInformationExtraField
        : ExtraField
    {
        private ZipEntryHeaderType _headerType;
        private IReadOnlyArray<byte> _buffer;
        private IZip64ExtendedInformationExtraFieldValueSource _headerSource;
        private UInt64? _internalSize;
        private UInt64? _internalPackedSize;
        private UInt64? _internalRelatiiveHeaderOffset;
        private UInt32? _inernalDiskStartNumber;

        protected Zip64ExtendedInformationExtraField(ZipEntryHeaderType headerType)
            : base(ExtraFieldId)
        {
            _headerType = headerType;
            _buffer = null;
            _headerSource = null;
            _internalSize = null;
            _internalPackedSize = null;
            _internalRelatiiveHeaderOffset = null;
            _inernalDiskStartNumber = null;
        }

        public const UInt16 ExtraFieldId = 1;

        public override IReadOnlyArray<byte> GetData(ZipEntryHeaderType headerType)
        {
            if (headerType != _headerType)
                return null;
            if (RequiredZip64Zip64ExtendedInformationExtraField == false)
                return null;
            BuildInternalBuffer();
            return _buffer;
        }

        public override void SetData(ZipEntryHeaderType headerType, IReadOnlyArray<byte> data, int index, int count)
        {
            _buffer = data.GetSequence(index, count).ToArray().AsReadOnly();
            if (headerType == _headerType)
                ParseInternalBuffer();
        }

        internal IZip64ExtendedInformationExtraFieldValueSource ZipHeaderSource
        {
            get => _headerSource;

            set
            {
                _headerSource = value;
                ParseInternalBuffer();
            }
        }

        public bool RequiredZip64Zip64ExtendedInformationExtraField =>
            _internalSize.HasValue ||
            _internalPackedSize.HasValue ||
            _internalRelatiiveHeaderOffset.HasValue ||
            _inernalDiskStartNumber.HasValue;

        protected UInt64 InternalSize
        {
            get
            {
                if (_headerSource == null)
                    throw new InvalidOperationException("HeaderSource is not set in Zip64 extra field.");
                if (_headerSource.Size == UInt32.MaxValue)
                {
                    if (_internalSize == null)
                        throw new InvalidOperationException();
                    return _internalSize.Value;
                }
                else
                {
                    if (_internalSize.HasValue)
                        throw new InvalidOperationException();
                    return _headerSource.Size;
                }
            }

            set
            {
                if (_headerSource == null)
                    throw new InvalidOperationException("HeaderSource is not set in Zip64 extra field.");
                if (value < UInt32.MaxValue)
                {
                    _internalSize = null;
                    _headerSource.Size = (UInt32)value;
                }
                else
                {
                    _internalSize = value;
                    _headerSource.Size = UInt32.MaxValue;
                }
            }
        }

        protected UInt64 InternalPackedSize
        {
            get
            {
                if (_headerSource == null)
                    throw new InvalidOperationException("HeaderSource is not set in Zip64 extra field.");
                if (_headerSource.PackedSize == UInt32.MaxValue)
                {
                    if (_internalPackedSize == null)
                        throw new InvalidOperationException();
                    return _internalPackedSize.Value;
                }
                else
                {
                    if (_internalPackedSize.HasValue)
                        throw new InvalidOperationException();
                    return _headerSource.PackedSize;
                }
            }

            set
            {
                if (_headerSource == null)
                    throw new InvalidOperationException("HeaderSource is not set in Zip64 extra field.");
                if (value < UInt32.MaxValue)
                {
                    _internalPackedSize = null;
                    _headerSource.PackedSize = (UInt32)value;
                }
                else
                {
                    _internalPackedSize = value;
                    _headerSource.PackedSize = UInt32.MaxValue;
                }
            }
        }

        protected UInt64 InternalRelativeHeaderOffset
        {
            get
            {
                if (_headerType != ZipEntryHeaderType.CentralDirectoryHeader)
                    throw new InvalidOperationException("RelativeHeaderOffset is only accessible in the central directory header.");
                if (_headerSource == null)
                    throw new InvalidOperationException("HeaderSource is not set in Zip64 extra field.");
                if (_headerSource.RelativeHeaderOffset == UInt32.MaxValue)
                {
                    if (_internalRelatiiveHeaderOffset == null)
                        throw new InvalidOperationException();
                    return _internalRelatiiveHeaderOffset.Value;
                }
                else
                {
                    if (_internalRelatiiveHeaderOffset.HasValue)
                        throw new InvalidOperationException();
                    return _headerSource.RelativeHeaderOffset;
                }
            }

            set
            {
                if (_headerType != ZipEntryHeaderType.CentralDirectoryHeader)
                    throw new InvalidOperationException();
                if (_headerSource == null)
                    throw new InvalidOperationException("HeaderSource is not set in Zip64 extra field.");
                if (value < UInt32.MaxValue)
                {
                    _internalRelatiiveHeaderOffset = null;
                    _headerSource.RelativeHeaderOffset = (UInt32)value;
                }
                else
                {
                    _internalRelatiiveHeaderOffset = value;
                    _headerSource.RelativeHeaderOffset = UInt32.MaxValue;
                }
            }
        }

        protected UInt32 InternalDiskStartNumber
        {
            get
            {
                if (_headerType != ZipEntryHeaderType.CentralDirectoryHeader)
                    throw new InvalidOperationException("DiskStartNumber is only accessible in the central directory header.");
                if (_headerSource == null)
                    throw new InvalidOperationException("HeaderSource is not set in Zip64 extra field.");
                if (_headerSource.DiskStartNumber == UInt16.MaxValue)
                {
                    if (_inernalDiskStartNumber == null)
                        throw new InvalidOperationException();
                    return _inernalDiskStartNumber.Value;
                }
                else
                {
                    if (_inernalDiskStartNumber.HasValue)
                        throw new InvalidOperationException();
                    return _headerSource.DiskStartNumber;
                }
            }

            set
            {
                if (_headerType != ZipEntryHeaderType.CentralDirectoryHeader)
                    throw new InvalidOperationException("DiskStartNumber is only accessible in the central directory header.");
                if (_headerSource == null)
                    throw new InvalidOperationException("HeaderSource is not set in Zip64 extra field.");
                if (value < UInt16.MaxValue)
                {
                    _inernalDiskStartNumber = null;
                    _headerSource.DiskStartNumber = (UInt16)value;
                }
                else
                {
                    _inernalDiskStartNumber = value;
                    _headerSource.DiskStartNumber = UInt16.MaxValue;
                }
            }
        }

        protected abstract void GetData(ByteArrayOutputStream writer);

        protected abstract void SetData(ByteArrayInputStream reader, UInt16 totalCount);

        private void BuildInternalBuffer()
        {
            if (_headerSource == null)
                throw new InvalidOperationException("HeaderSource is not set in Zip64 extra field.");
            var writer = new ByteArrayOutputStream();
            GetData(writer);
            var newBuffer = writer.ToByteArray();
            _buffer = newBuffer.Length > 0 ? newBuffer : null;
        }

        private void ParseInternalBuffer()
        {
            if (_headerSource != null && _buffer != null)
            {
                _internalSize = null;
                _internalPackedSize = null;
                _internalRelatiiveHeaderOffset = null;
                _inernalDiskStartNumber = null;
                var reader = new ByteArrayInputStream(_buffer);
                var success = false;
                try
                {
                    SetData(reader, (UInt16)_buffer.Length);
                    if (reader.ReadAllBytes().Length > 0)
                        throw GetBadFormatException(_headerType, _buffer, 0, _buffer.Length);
                    success = true;
                }
                catch (UnexpectedEndOfStreamException)
                {
                    throw GetBadFormatException(_headerType, _buffer, 0, _buffer.Length);
                }
                finally
                {
                    if (!success)
                    {
                        _internalSize = null;
                        _internalPackedSize = null;
                        _internalRelatiiveHeaderOffset = null;
                        _inernalDiskStartNumber = null;
                    }
                }
            }
        }
    }
}