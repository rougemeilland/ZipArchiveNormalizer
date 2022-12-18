using System;

namespace Utility.IO
{
    abstract class BufferedRandomOutputStream<POSITION_T>
        : BufferedOutputStream<POSITION_T>
    {
        private readonly IRandomOutputByteStream<POSITION_T> _baseStream;

        public BufferedRandomOutputStream(IRandomOutputByteStream<POSITION_T> baseStream, bool leaveOpen)
            : base(baseStream, leaveOpen)
        {
            _baseStream = baseStream;
        }

        public BufferedRandomOutputStream(IRandomOutputByteStream<POSITION_T> baseStream, Int32 bufferSize, bool leaveOpen)
            : base(baseStream, bufferSize, leaveOpen)
        {
            _baseStream = baseStream;
        }

        public UInt64 Length
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                if (CachedDataLength > 0)
                    return _baseStream.Length.Maximum(GetDistanceBetweenPositions(_baseStream.Position, ZeroPositionValue) + (UInt32)CachedDataLength);
                else
                    return _baseStream.Length;
            }

            set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                InternalFlush();
                _baseStream.Length = value;
            }
        }

        public void Seek(POSITION_T offset)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            InternalFlush();
            _baseStream.Seek(offset);
            SetPosition(GetDistanceBetweenPositions(offset, ZeroPositionValue));
        }

        protected abstract UInt64 GetDistanceBetweenPositions(POSITION_T x, POSITION_T y);
    }
}
