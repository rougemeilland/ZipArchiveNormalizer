using System;

namespace Utility.IO.Compression.Lz
{
    class InWindow
    {
        private IInputByteStream<UInt64> _stream;
        private UInt32 _posLimit; // offset (from _buffer) of first byte when new block reading must be done
        private bool _streamEndWasReached; // if (true) then _streamPos shows real end of stream

        private UInt32 _pointerToLastSafePosition;

        private UInt32 _blockSize; // Size of Allocated memory block
        private UInt32 _keepSizeBefore; // how many BYTEs must be kept in buffer before _pos
        private UInt32 _keepSizeAfter; // how many BYTEs must be kept buffer after _pos

        protected UInt32 Position; // offset (from _buffer) of curent byte
        protected Byte[] BufferBase; // pointer to buffer with data
        protected UInt32 BufferOffset;
        protected UInt32 StreamPosition; // offset (from _buffer) of first not read byte from Stream

        public InWindow()
        {
            BufferBase = null;
        }

        public void MoveBlock()
        {
            var offset = BufferOffset + Position - _keepSizeBefore;
            // we need one additional byte, since MovePos moves on 1 byte.
            if (offset > 0)
                offset--;
            
            var numBytes = BufferOffset + StreamPosition - offset;

            // check negative offset ????
            for (var i = 0; i < numBytes; i++)
                BufferBase[i] = BufferBase[offset + i];
            BufferOffset -= offset;
        }

        public virtual void ReadBlock()
        {
            if (_streamEndWasReached)
                return;
            while (true)
            {
                int size = (int)((0 - BufferOffset) + _blockSize - StreamPosition);
                if (size == 0)
                    return;
                int numReadBytes = _stream.Read(BufferBase, (int)(BufferOffset + StreamPosition), size);
                if (numReadBytes == 0)
                {
                    _posLimit = StreamPosition;
                    UInt32 pointerToPostion = BufferOffset + _posLimit;
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

        public void Create(UInt32 keepSizeBefore, UInt32 keepSizeAfter, UInt32 keepSizeReserv)
        {
            _keepSizeBefore = keepSizeBefore;
            _keepSizeAfter = keepSizeAfter;
            UInt32 blockSize = keepSizeBefore + keepSizeAfter + keepSizeReserv;
            if (BufferBase == null || _blockSize != blockSize)
            {
                _blockSize = blockSize;
                BufferBase = new Byte[_blockSize];
            }
            _pointerToLastSafePosition = _blockSize - keepSizeAfter;
        }

        public void SetStream(IInputByteStream<UInt64> stream)
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
            Position++;
            if (Position > _posLimit)
            {
                UInt32 pointerToPostion = BufferOffset + Position;
                if (pointerToPostion > _pointerToLastSafePosition)
                    MoveBlock();
                ReadBlock();
            }
        }

        public Byte GetIndexByte(Int32 index) { return BufferBase[BufferOffset + Position + index]; }

        // index + limit have not to exceed _keepSizeAfter;
        public UInt32 GetMatchLen(Int32 index, UInt32 distance, UInt32 limit)
        {
            if (_streamEndWasReached)
                if ((Position + index) + limit > StreamPosition)
                    limit = StreamPosition - (UInt32)(Position + index);
            distance++;
            // Byte *pby = _buffer + (size_t)_pos + index;
            UInt32 pby = BufferOffset + Position + (UInt32)index;

            UInt32 i;
            for (i = 0; i < limit && BufferBase[pby + i] == BufferBase[pby + i - distance]; i++);
            return i;
        }

        public UInt32 GetNumAvailableBytes() { return StreamPosition - Position; }

        public void ReduceOffsets(Int32 subValue)
        {
            BufferOffset += (UInt32)subValue;
            _posLimit -= (UInt32)subValue;
            Position -= (UInt32)subValue;
            StreamPosition -= (UInt32)subValue;
        }
    }
}
