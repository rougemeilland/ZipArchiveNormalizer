using System;
using System.Collections;
using System.Collections.Generic;

namespace Utility.IO
{
    public abstract class ReverseByteSequenceByByteStreamEnumerable<POSITION_T>
        : IEnumerable<byte>
        where POSITION_T: IComparable<POSITION_T>
    {
        private class Enumerator
            : IEnumerator<byte>
        {
            private const int _bufferSize = 64 * 1024;
            private bool _isDisposed;
            private ReverseByteSequenceByByteStreamEnumerable<POSITION_T> _parent;
            private IRandomInputByteStream<POSITION_T> _inputStream;
            private POSITION_T _offset;
            private UInt64 _count;
            private bool _leaveOpen;
            private Action<int> _progressAction;
            private byte[] _buffer;
            private int _bufferCount;
            private int _bufferIndex;
            private POSITION_T _fileIndex;

            public Enumerator(ReverseByteSequenceByByteStreamEnumerable<POSITION_T> parent, IRandomInputByteStream<POSITION_T> randomAccessStream, POSITION_T offset, UInt64 count, bool leaveOpen, Action<int> progressAction)
            {
                _isDisposed = false;
                _parent = parent;
                _inputStream = randomAccessStream;
                _offset = offset;
                _count = count;
                _leaveOpen = leaveOpen;
                _progressAction = progressAction;
                _buffer = new byte[_bufferSize];
                _bufferCount = 0;
                _bufferIndex = 0;
                _fileIndex = _parent.AddPositionAndDistance(_offset, _count);
            }

            public byte Current
            {
                get
                {
                    if (_isDisposed)
                        throw new ObjectDisposedException(GetType().FullName);
                    if (_bufferIndex.IsBetween(0, _bufferCount - 1) == false)
                        throw new InvalidOperationException();

                    return _buffer[_bufferIndex];
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                if (_bufferIndex <= 0)
                {
                    var newFileIndex =
                        _fileIndex.CompareTo(_parent.AddPositionAndDistance(_offset, _bufferSize)) < 0
                        ? _offset
                        : _parent.SubtractBufferSizeFromPosition(_fileIndex, _bufferSize);
                    _bufferCount = _parent.GetDistanceBetweenPositions(_fileIndex, newFileIndex);
                    if (_bufferCount <= 0)
                        return false;
                    _fileIndex = newFileIndex;
                    _inputStream.Seek(_fileIndex);
                    _inputStream.ReadBytes(_buffer, 0, _bufferCount);
                    _bufferIndex = _bufferCount;
                    if (_progressAction != null)
                    {
                        try
                        {
                            _progressAction(_bufferCount);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                --_bufferIndex;
                return true;
            }

            public void Reset()
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                _bufferCount = 0;
                _bufferIndex = 0;
                _fileIndex = _parent.AddPositionAndDistance(_offset, _count);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_isDisposed)
                {
                    if (disposing)
                    {
                        if (_inputStream != null)
                        {
                            if (_leaveOpen == false)
                                _inputStream.Dispose();
                            _inputStream = null;
                        }
                    }
                    _isDisposed = true;
                }
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        private IRandomInputByteStream<POSITION_T> _baseStream;
        private POSITION_T _offset;
        private UInt64 _count;
        private bool _leaveOpen;
        private Action<int> _progressAction;

        public ReverseByteSequenceByByteStreamEnumerable(IRandomInputByteStream<POSITION_T> baseStream, POSITION_T offset, UInt64 count, bool leaveOpen, Action<int> progressAction)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                _baseStream = baseStream;
                _offset = offset;
                _count = count;
                _leaveOpen = leaveOpen;
                _progressAction = progressAction;

                if (_baseStream == null)
                    throw new NotSupportedException();
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        protected abstract POSITION_T AddPositionAndDistance(POSITION_T position, UInt64 distance);
        protected abstract POSITION_T SubtractBufferSizeFromPosition(POSITION_T position, uint distance);
        protected abstract int GetDistanceBetweenPositions(POSITION_T position1, POSITION_T position2);
        public IEnumerator<byte> GetEnumerator() => new Enumerator(this, _baseStream, _offset, _count, _leaveOpen, _progressAction);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
