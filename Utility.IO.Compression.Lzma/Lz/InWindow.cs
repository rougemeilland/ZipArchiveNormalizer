using System;

namespace Utility.IO.Compression.Lzma.Lz
{
    class InWindow
    {
        private IInputBuffer _stream;
        private UInt32 _posLimit; // offset (from _buffer) of first byte when new block reading must be done
        private bool _streamEndWasReached; // if (true) then _streamPos shows real end of stream

        private UInt32 _pointerToLastSafePosition;

        private UInt32 _blockSize; // Size of Allocated memory block
        private UInt32 _keepSizeBefore; // how many BYTEs must be kept in buffer before _pos
        private UInt32 _keepSizeAfter; // how many BYTEs must be kept buffer after _pos

        protected UInt32 Position; // offset (from _buffer) of curent byte
        protected Byte[] BufferBase; // pointer to buffer with data
        protected Int64 BufferOffset;
        protected UInt32 StreamPosition; // offset (from _buffer) of first not read byte from Stream

        public InWindow()
        {
            BufferBase = null;
        }

        public void MoveBlock()
        {
#if DEBUG
            checked
#endif
            {
                if (BufferOffset + Position >= _keepSizeBefore)
                {
                    var offset = BufferOffset + Position - _keepSizeBefore;
                    // we need one additional byte, since MovePos moves on 1 byte.
                    if (offset > 0)
                        offset--;

                    var numBytes = BufferOffset + StreamPosition - offset;
                    BufferBase.CopyTo((int)offset, BufferBase, 0, (int)numBytes);
                    BufferOffset -= offset;
                }
            }
        }

        public virtual void ReadBlock()
        {
            if (_streamEndWasReached)
                return;
            while (true)
            {
#if DEBUG
                checked
#endif
                {
                    var offset = BufferOffset + StreamPosition;
                    if (offset >= _blockSize)
                        return;
                    int numReadBytes = _stream.Read(BufferBase, (int)offset, (int)(_blockSize - offset));
                    if (numReadBytes <= 0)
                    {
                        _posLimit = StreamPosition;
                        var pointerToPostion = BufferOffset + _posLimit;
                        if (pointerToPostion > _pointerToLastSafePosition)
                            _posLimit = (UInt32)(_pointerToLastSafePosition - BufferOffset);

                        _streamEndWasReached = true;
                        return;
                    }
                    StreamPosition += (UInt32)numReadBytes;
                    if (StreamPosition >= Position + _keepSizeAfter)
                        _posLimit = StreamPosition - _keepSizeAfter;
                }
            }
        }

        public void Create(UInt32 keepSizeBefore, UInt32 keepSizeAfter, UInt32 keepSizeReserv)
        {
#if DEBUG
            checked
#endif
            {
                _keepSizeBefore = keepSizeBefore;
                _keepSizeAfter = keepSizeAfter;
                var blockSize = keepSizeBefore + keepSizeAfter + keepSizeReserv;
                if (BufferBase == null || _blockSize != blockSize)
                {
                    _blockSize = blockSize;
                    BufferBase = new Byte[_blockSize];
                }
                _pointerToLastSafePosition = _blockSize - keepSizeAfter;
            }
        }

        public void SetStream(IInputBuffer stream)
        {
            _stream = stream;
        }

        public void ReleaseStream()
        {
            _stream = null;
        }

        public virtual void Init()
        {
            BufferOffset = 0;
            Position = 0;
            StreamPosition = 0;
            _streamEndWasReached = false;
            ReadBlock();
        }

        public virtual void MovePos()
        {
#if DEBUG
            checked
#endif
            {
                Position++;
                if (Position > _posLimit)
                {
                    var pointerToPostion = BufferOffset + Position;
                    if (pointerToPostion > _pointerToLastSafePosition)
                        MoveBlock();
                    ReadBlock();
                }
            }
        }

        public Byte GetIndexByte(Int32 index)
        {
#if DEBUG
            checked
#endif
            {
                return BufferBase[BufferOffset + Position + index];
            }
        }

        // index + limit have not to exceed _keepSizeAfter;
        public UInt32 GetMatchLen(Int32 index, UInt32 distance, UInt32 limit)
        {
#if DEBUG
            checked
#endif
            {
                if (_streamEndWasReached)
                    if (Position + index + limit > StreamPosition)
                        limit = StreamPosition - (UInt32)(Position + index);
                distance++;
                var pby = BufferOffset + Position + index;
                var i = 0U;
                while (i < limit && BufferBase[pby + i] == BufferBase[pby + i - distance])
                    ++i;
                return i;
            }
        }

        public UInt32 GetNumAvailableBytes()
        {
#if DEBUG
            checked
#endif
            {
                return StreamPosition - Position;
            }
        }

        public void ReduceOffsets(Int32 subValue)
        {
#if DEBUG
            checked
#endif
            {
                BufferOffset += subValue;
                _posLimit = (UInt32)(_posLimit - subValue);
                Position = (UInt32)(Position - subValue);
                StreamPosition = (UInt32)(StreamPosition - subValue);
            }
        }

        public UInt32 BlockSize => _blockSize;
    }
}
