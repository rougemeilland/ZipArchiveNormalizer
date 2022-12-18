using System;
using Utility;
using Utility.IO;
using ZipUtility.ZipFileHeader;

namespace ZipUtility.ZipExtraField
{
    abstract class Zip64ExtendedInformationExtraField
        : ExtraField
    {
        private readonly ZipEntryHeaderType _headerType;

        private ReadOnlyMemory<byte>? _buffer;
        private IZip64ExtendedInformationExtraFieldValueSource? _headerSource;
        private bool _isParsedInternalBuffer;
        private UInt64? _internalSize;
        private UInt64? _internalPackedSize;
        private UInt64? _internalRelatiiveHeaderOffset;
        private UInt32? _inernalDiskStartNumber;

        protected Zip64ExtendedInformationExtraField(ZipEntryHeaderType headerType)
            : base(ExtraFieldId)
        {
            _headerType = headerType;
            _buffer = Array.Empty<byte>().AsReadOnly();
            _headerSource = null;
            _isParsedInternalBuffer = false;
            _internalSize = null;
            _internalPackedSize = null;
            _internalRelatiiveHeaderOffset = null;
            _inernalDiskStartNumber = null;
        }

        public const UInt16 ExtraFieldId = 1;

        public override ReadOnlyMemory<byte>? GetData(ZipEntryHeaderType headerType)
        {
            if (headerType != _headerType)
                return null;
            if (!RequiredZip64Zip64ExtendedInformationExtraField)
                return null;
            var newBuffer = BuildInternalBuffer();
            return newBuffer.Length > 0 ? newBuffer : null;
        }

        public override void SetData(ZipEntryHeaderType headerType, ReadOnlyMemory<byte> data)
        {
            _buffer =
                headerType == _headerType
                ? data.ToArray()
                : null;
            _isParsedInternalBuffer = false;
        }

        internal void SetZipHeaderSource(IZip64ExtendedInformationExtraFieldValueSource zipHeaderSource)
        {
            _headerSource = zipHeaderSource;
            _isParsedInternalBuffer = false;
        }

        public bool RequiredZip64Zip64ExtendedInformationExtraField =>
            _internalSize is not null ||
            _internalPackedSize is not null ||
            _internalRelatiiveHeaderOffset is not null ||
            _inernalDiskStartNumber is not null;

        protected IZip64ExtendedInformationExtraFieldValueSource ZipHeaderSource
        {
            get
            {
                if (_headerSource is null)
                    throw new InvalidOperationException("HeaderSource is not set in Zip64 extra field.");

                return _headerSource;
            }
        }

        protected UInt64 InternalSize
        {
            get
            {
                ParseInternalBuffer();
                if (_headerSource is null)
                    throw new InvalidOperationException("HeaderSource is not set in Zip64 extra field.");
                if (_headerSource.Size == UInt32.MaxValue)
                {
                    if (_internalSize is null)
                        throw new InvalidOperationException();
                    return _internalSize.Value;
                }
                else
                {
                    if (_internalSize is not null)
                        throw new InvalidOperationException();
                    return _headerSource.Size;
                }
            }

            set
            {
                if (_headerSource is null)
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
                ParseInternalBuffer();
                if (_headerSource is null)
                    throw new InvalidOperationException("HeaderSource is not set in Zip64 extra field.");
                if (_headerSource.PackedSize == UInt32.MaxValue)
                {
                    if (_internalPackedSize is null)
                        throw new InvalidOperationException();
                    return _internalPackedSize.Value;
                }
                else
                {
                    if (_internalPackedSize is not null)
                        throw new InvalidOperationException();
                    return _headerSource.PackedSize;
                }
            }

            set
            {
                if (_headerSource is null)
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
                ParseInternalBuffer();
                if (_headerType != ZipEntryHeaderType.CentralDirectoryHeader)
                    throw new InvalidOperationException("RelativeHeaderOffset is only accessible in the central directory header.");
                if (_headerSource is null)
                    throw new InvalidOperationException("HeaderSource is not set in Zip64 extra field.");
                if (_headerSource.RelativeHeaderOffset == UInt32.MaxValue)
                {
                    if (_internalRelatiiveHeaderOffset is null)
                        throw new InvalidOperationException();
                    return _internalRelatiiveHeaderOffset.Value;
                }
                else
                {
                    if (_internalRelatiiveHeaderOffset is not null)
                        throw new InvalidOperationException();
                    return _headerSource.RelativeHeaderOffset;
                }
            }

            set
            {
                if (_headerType != ZipEntryHeaderType.CentralDirectoryHeader)
                    throw new InvalidOperationException();
                if (_headerSource is null)
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
                ParseInternalBuffer();
                if (_headerType != ZipEntryHeaderType.CentralDirectoryHeader)
                    throw new InvalidOperationException("DiskStartNumber is only accessible in the central directory header.");
                if (_headerSource is null)
                    throw new InvalidOperationException("HeaderSource is not set in Zip64 extra field.");
                if (_headerSource.DiskStartNumber == UInt16.MaxValue)
                {
                    if (_inernalDiskStartNumber is null)
                        throw new InvalidOperationException();
                    return _inernalDiskStartNumber.Value;
                }
                else
                {
                    if (_inernalDiskStartNumber is not null)
                        throw new InvalidOperationException();
                    return _headerSource.DiskStartNumber;
                }
            }

            set
            {
                if (_headerType != ZipEntryHeaderType.CentralDirectoryHeader)
                    throw new InvalidOperationException("DiskStartNumber is only accessible in the central directory header.");
                if (_headerSource is null)
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

        protected abstract void GetData(ByteArrayRenderer writer);

        protected abstract void SetData(ByteArrayParser reader, UInt16 totalCount);

        private ReadOnlyMemory<byte> BuildInternalBuffer()
        {
            if (_headerSource is null)
                throw new InvalidOperationException("HeaderSource is not set in Zip64 extra field.");
            var writer = new ByteArrayRenderer();
            GetData(writer);
            var newBuffer = writer.ToByteArray();
            _buffer = newBuffer;
            return newBuffer;
        }

        private void ParseInternalBuffer()
        {
            if (_headerSource is not null && _buffer.HasValue && !_isParsedInternalBuffer)
            {
                _internalSize = null;
                _internalPackedSize = null;
                _internalRelatiiveHeaderOffset = null;
                _inernalDiskStartNumber = null;
                var reader = new ByteArrayParser(_buffer.Value);
                var success = false;
                try
                {
                    SetData(reader, (UInt16)_buffer.Value.Length);
                    if (reader.ReadAllBytes().Length > 0)
                        throw GetBadFormatException(_headerType, _buffer.Value);
                    success = true;
                    _isParsedInternalBuffer = true;
                }
                catch (UnexpectedEndOfStreamException)
                {
                    throw GetBadFormatException(_headerType, _buffer.Value);
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
