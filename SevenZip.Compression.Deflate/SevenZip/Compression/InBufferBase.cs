// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using Utility;
using Utility.IO;

namespace SevenZip.Compression
{
    abstract class InBufferBase
    {
        protected Byte[] _bufBase;
        protected Int32 _bufOffset;
        protected Int32 _bufLim;
        protected UInt64 _processedSize;
        protected bool _wasFinished;
        private UInt32 _numExtraBytes;

        protected InBufferBase(UInt32 bufSize)
        {
            _bufBase = new byte[bufSize.Maximum(1U)];
            _bufOffset = 0;
            _bufLim = 0;
            _processedSize = 0;
            _wasFinished = false;
            _numExtraBytes = 0;
        }

        public UInt64 StreamSize => _processedSize + (UInt32)_bufOffset;
        public UInt64 ProcessedSize => _processedSize + _numExtraBytes + (UInt32)_bufOffset;
        public UInt32 NumExtraBytes => _numExtraBytes;
        public bool WasFinished => _wasFinished;

        public Byte ReadByte(IBasicInputByteStream inStream)
        {
            if (inStream is null)
                throw new ArgumentNullException(nameof(inStream));

            if (_bufOffset >= _bufLim)
                return ReadByteFromNewBlock(inStream);
            return _bufBase[_bufOffset++];
        }

        public bool ReadByte(IBasicInputByteStream inStream, out Byte data)
        {
            if (inStream is null)
                throw new ArgumentNullException(nameof(inStream));

            if (_bufOffset >= _bufLim)
                return ReadByteFromNewBlock(inStream, out data);
            data = _bufBase[_bufOffset++];
            return true;
        }

        public bool ReadByteFromBuf(out Byte data)
        {
            if (_bufOffset >= _bufLim)
            {
                data = 0;
                return false;
            }
            data = _bufBase[_bufOffset++];
            return true;
        }

        public UInt32 Skip(IBasicInputByteStream inStream, UInt32 size)
        {
            if (inStream is null)
                throw new ArgumentNullException(nameof(inStream));

            var processed = 0;
            while (true)
            {
                var rem = _bufLim - _bufOffset;
#if DEBUG
                if (rem < 0)
                    throw new Exception();
#endif
                if (rem >= size)
                {
                    _bufOffset += (Int32)size;
                    return (UInt32)(processed + size);
                }
                _bufOffset += rem;
                processed += rem;
                size -= (UInt32)rem;
                if (!ReadBlock(inStream))
                    return (UInt32)processed;
            }
        }

        protected bool ReadBlock(IBasicInputByteStream inStream)
        {
            if (inStream is null)
                throw new ArgumentNullException(nameof(inStream));

            if (_wasFinished)
                return false;
            var processed = inStream.ReadBytes(_bufBase, 0, _bufBase.Length);
            _bufOffset = 0;
            _bufLim = _bufOffset + processed;
            _processedSize += (UInt32)_bufOffset;
            _wasFinished = processed <= 0;
            return !_wasFinished;
        }

        protected Byte ReadByteFromNewBlock(IBasicInputByteStream inStream)
        {
            if (inStream is null)
                throw new ArgumentNullException(nameof(inStream));

            if (!ReadBlock(inStream))
            {
                ++_numExtraBytes;
                return Byte.MaxValue;
            }
            var data = _bufBase[_bufOffset++];
            return data;
        }

        protected bool ReadByteFromNewBlock(IBasicInputByteStream inStream, out Byte data)
        {
            if (inStream is null)
                throw new ArgumentNullException(nameof(inStream));

            if (!ReadBlock(inStream))
            {
                ++_numExtraBytes;
                data = Byte.MaxValue;
                return false;
            }
            data = _bufBase[_bufOffset++];
            return true;
        }
    }
}
