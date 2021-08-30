using System;
using System.Linq;
using Utility;
using ZipUtility.Helper;
using ZipUtility.ZipFileHeader;

namespace ZipUtility.ZipExtraField
{
    abstract class Zip64ExtendedInformationExtraField
        : ExtraField
    {
        private ZipEntryHeaderType _headerType;
        private byte[] _buffer;
        private IZip64ExtendedInformationExtraFieldValueSource _source;
        private Int64? _internalSize;
        private Int64? _internalPackedSize;
        private Int64? _internalRelatiiveHeaderOffset;
        private UInt32? _inernalDiskStartNumber;

        protected Zip64ExtendedInformationExtraField(ZipEntryHeaderType headerType)
            : base(ExtraFieldId)
        {
            _headerType = headerType;
            _buffer = null;
            _source = null;
            _internalSize = null;
            _internalPackedSize = null;
            _internalRelatiiveHeaderOffset = null;
            _inernalDiskStartNumber = null;
        }

        public const UInt16 ExtraFieldId = 1;

        public override byte[] GetData(ZipEntryHeaderType headerType)
        {
            if (headerType != _headerType)
                return null;
            BuildInternalBuffer(_headerType);
            return _buffer;
        }

        public override void SetData(ZipEntryHeaderType headerType, byte[] data, int index, int count)
        {
            _buffer = data.GetSequence(index, count).ToArray();
            if (headerType == _headerType)
                ParseInternalBuffer(_headerType);
        }

        internal IZip64ExtendedInformationExtraFieldValueSource Source
        {
            get => _source;

            set
            {
                _source = value;
                ParseInternalBuffer(_headerType);
            }
        }

        public bool RequiredZip64Zip64ExtendedInformationExtraField =>
            _internalSize.HasValue ||
            _internalPackedSize.HasValue ||
            _internalRelatiiveHeaderOffset.HasValue ||
            _inernalDiskStartNumber.HasValue;

        protected Int64 InternalSize
        {
            get
            {
                if (_source == null)
                    throw new InvalidOperationException("Source is not set");
                if (_source.Size == UInt32.MaxValue)
                {
                    if (!_internalSize.HasValue)
                        throw new InvalidOperationException();
                    return _internalSize.Value;
                }
                else
                {
                    if (_internalSize.HasValue || !_source.Size.HasValue)
                        throw new InvalidOperationException();
                    return _source.Size.Value;
                }
            }

            set
            {
                if (_source == null)
                    throw new InvalidOperationException("Source is not set");
                if (value < UInt32.MaxValue)
                {
                    _internalSize = null;
                    _source.Size = (UInt32)value;
                }
                else
                {
                    _internalSize = value;
                    _source.Size = UInt32.MaxValue;
                }
            }
        }

        protected Int64 InternalPackedSize
        {
            get
            {
                if (_source == null)
                    throw new InvalidOperationException("Source is not set");
                if (_source.PackedSize == UInt32.MaxValue)
                {
                    if (!_internalPackedSize.HasValue)
                        throw new InvalidOperationException();
                    return _internalPackedSize.Value;
                }
                else
                {
                    if (_internalPackedSize.HasValue || !_source.PackedSize.HasValue)
                        throw new InvalidOperationException();
                    return _source.PackedSize.Value;
                }
            }

            set
            {
                if (_source == null)
                    throw new InvalidOperationException("Source is not set");
                if (value < UInt32.MaxValue)
                {
                    _internalPackedSize = null;
                    _source.PackedSize = (UInt32)value;
                }
                else
                {
                    _internalPackedSize = value;
                    _source.PackedSize = UInt32.MaxValue;
                }
            }
        }

        protected Int64 InternalRelativeHeaderOffset
        {
            get
            {
                if (_headerType != ZipEntryHeaderType.CentralDirectoryHeader)
                    throw new InvalidOperationException();
                if (_source == null)
                    throw new InvalidOperationException("Source is not set");
                if (_source.RelativeHeaderOffset == UInt32.MaxValue)
                {
                    if (!_internalRelatiiveHeaderOffset.HasValue)
                        throw new InvalidOperationException();
                    return _internalRelatiiveHeaderOffset.Value;
                }
                else
                {
                    if (_internalRelatiiveHeaderOffset.HasValue || !_source.RelativeHeaderOffset.HasValue)
                        throw new InvalidOperationException();
                    return _source.RelativeHeaderOffset.Value;
                }
            }

            set
            {
                if (_headerType != ZipEntryHeaderType.CentralDirectoryHeader)
                    throw new InvalidOperationException();
                if (_source == null)
                    throw new InvalidOperationException("Source is not set");
                if (value < UInt32.MaxValue)
                {
                    _internalRelatiiveHeaderOffset = null;
                    _source.RelativeHeaderOffset = (UInt32)value;
                }
                else
                {
                    _internalRelatiiveHeaderOffset = value;
                    _source.RelativeHeaderOffset = UInt32.MaxValue;
                }
            }
        }

        protected UInt32 InternalDiskStartNumber
        {
            get
            {
                if (_headerType != ZipEntryHeaderType.CentralDirectoryHeader)
                    throw new InvalidOperationException();
                if (_source == null)
                    throw new InvalidOperationException("Source is not set");
                if (_source.DiskStartNumber == UInt16.MaxValue)
                {
                    if (!_inernalDiskStartNumber.HasValue)
                        throw new InvalidOperationException();
                    return _inernalDiskStartNumber.Value;
                }
                else
                {
                    if (_inernalDiskStartNumber.HasValue || !_source.DiskStartNumber.HasValue)
                        throw new InvalidOperationException();
                    return _source.DiskStartNumber.Value;
                }
            }

            set
            {
                if (_headerType != ZipEntryHeaderType.CentralDirectoryHeader)
                    throw new InvalidOperationException();
                if (_source == null)
                    throw new InvalidOperationException("Source is not set");
                if (value < UInt16.MaxValue)
                {
                    _inernalDiskStartNumber = null;
                    _source.DiskStartNumber = (UInt16)value;
                }
                else
                {
                    _inernalDiskStartNumber = value;
                    _source.DiskStartNumber = UInt16.MaxValue;
                }
            }
        }

        private void ParseInternalBuffer(ZipEntryHeaderType headerType)
        {
            if (_source != null && _buffer != null)
            {
                var reader = new ByteArrayInputStream(_buffer);

                if (_source.Size.HasValue)
                    InternalSize = _source.Size == UInt32.MaxValue ? reader.ReadInt64LE() : _source.Size.Value;
                else
                    _internalSize = null;

                if (_source.PackedSize.HasValue)
                    InternalPackedSize = _source.PackedSize == UInt32.MaxValue ? reader.ReadInt64LE() : _source.PackedSize.Value;
                else
                    _internalPackedSize = null;

                if (headerType == ZipEntryHeaderType.CentralDirectoryHeader)
                {

                    if (_source.RelativeHeaderOffset.HasValue)
                        InternalRelativeHeaderOffset = _source.RelativeHeaderOffset == UInt32.MaxValue ? reader.ReadInt64LE() : _source.RelativeHeaderOffset.Value;
                    else
                        _internalRelatiiveHeaderOffset = null;

                    if (_source.DiskStartNumber.HasValue)
                        InternalDiskStartNumber = _source.DiskStartNumber == UInt16.MaxValue ? reader.ReadUInt32LE() : _source.DiskStartNumber.Value;
                    else
                        _inernalDiskStartNumber = null;
                }
            }
        }

        private void BuildInternalBuffer(ZipEntryHeaderType headerType)
        {
            if (_source == null)
            {
                _buffer = null;
                return;
            }
            var writer = new ByteArrayOutputStream();
            if (_source.Size.HasValue && _source.Size.Value == UInt32.MaxValue && _internalSize.HasValue)
                writer.WriteInt64LE(_internalSize.Value);
            if (_source.PackedSize.HasValue && _source.PackedSize.Value == UInt32.MaxValue && _internalPackedSize.HasValue)
                writer.WriteInt64LE(_internalPackedSize.Value);
            if (headerType == ZipEntryHeaderType.CentralDirectoryHeader)
            {
                if (_source.RelativeHeaderOffset.HasValue && _source.RelativeHeaderOffset.Value == UInt32.MaxValue && _internalRelatiiveHeaderOffset.HasValue)
                    writer.WriteInt64LE(_internalRelatiiveHeaderOffset.Value);
                if (_source.DiskStartNumber.HasValue && _source.DiskStartNumber.Value == UInt16.MaxValue && _inernalDiskStartNumber.HasValue)
                    writer.WriteUInt32LE(_inernalDiskStartNumber.Value);
            }
            var newBuffer = writer.ToByteSequence().ToArray();
            _buffer = newBuffer.Length > 0 ? newBuffer : null;
        }
    }
}