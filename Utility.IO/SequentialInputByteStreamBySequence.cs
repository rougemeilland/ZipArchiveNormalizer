using System;
using System.Collections.Generic;

namespace Utility.IO
{
    class SequentialInputByteStreamBySequence
        : IInputByteStream<UInt64>
    {
        private bool _isDisposed;
        private IEnumerator<byte> _sourceSequenceEnumerator;
        private ulong _position;
        private bool _isEndOfBaseStream;
        private bool _isEndOfStream;

        public SequentialInputByteStreamBySequence(IEnumerable<byte> sourceSequence)
        {
            _isDisposed = false;
            _sourceSequenceEnumerator = sourceSequence.GetEnumerator();
            _position = 0;
            _isEndOfBaseStream = false;
            _isEndOfStream = false;
        }

        public ulong Position => !_isDisposed ? _position : throw new ObjectDisposedException(GetType().FullName);

        public int Read(byte[] buffer, int offset, int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentException();
            if (count < 0)
                throw new ArgumentException();
            if (offset + count > buffer.Length)
                throw new ArgumentException();

            if (_isEndOfStream)
                return 0;

            var actualCount = 0;
            while (_isEndOfBaseStream == false && actualCount < count)
            {
                if (_sourceSequenceEnumerator.MoveNext())
                {
                    buffer[offset + actualCount] = _sourceSequenceEnumerator.Current;
                    ++actualCount;
                }
                else
                {
                    _isEndOfBaseStream = true;
                    break;
                }
            }
            if (actualCount <= 0)
            {
                _isEndOfStream = true;
                return 0;
            }
            _position += (ulong)actualCount;
            return actualCount;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_sourceSequenceEnumerator != null)
                    {
                        _sourceSequenceEnumerator.Dispose();
                        _sourceSequenceEnumerator = null;
                    }
                }
                _isDisposed = true;
            }
        }
    }
}
