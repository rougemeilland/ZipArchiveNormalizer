﻿using System;

namespace Utility.IO
{
    abstract class BufferedRandomInputStream<POSITION_T>
        : BufferedInputStream<POSITION_T>
    {
        private readonly IRandomInputByteStream<POSITION_T> _baseStream;

        public BufferedRandomInputStream(IRandomInputByteStream<POSITION_T> baseStream, bool leaveOpen)
            : base(baseStream, leaveOpen)
        {
            _baseStream = baseStream;
        }

        public BufferedRandomInputStream(IRandomInputByteStream<POSITION_T> baseStream, Int32 bufferSize, bool leaveOpen)
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

                return _baseStream.Length;
            }

            set => throw new NotSupportedException();
        }

        public void Seek(POSITION_T offset)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            ClearCache();
            _baseStream.Seek(offset);
            SetPosition(GetDistanceBetweenPositions(offset, ZeroPositionValue));
        }

        protected abstract UInt64 GetDistanceBetweenPositions(POSITION_T x, POSITION_T y);
    }
}
