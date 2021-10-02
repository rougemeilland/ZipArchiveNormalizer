using System;
using System.Collections;
using System.Collections.Generic;

namespace Utility.IO
{
    public class ByteSequenceByByteStreamEnumerable<POSITION_T>
        : IEnumerable<byte>
        where POSITION_T : struct
    {
        private class Enumerator
            : IEnumerator<byte>
        {
            private bool _isDisposed;
            private IInputByteStream<POSITION_T> _inputStream;
            private IRandomInputByteStream<POSITION_T> _randomAccessStream;
            private POSITION_T? _offset;
            private UInt64? _count;
            private bool _leaveOpen;
            private Action<int> _progressAction;
            private byte[] _buffer;
            private int _bufferCount;
            private int _bufferIndex;
            private UInt64 _index;

            public Enumerator(IInputByteStream<POSITION_T> inputStream, IRandomInputByteStream<POSITION_T> randomAccessStream, POSITION_T? offset, UInt64? count, bool leaveOpen, Action<int> progressAction)
            {
                _isDisposed = false;
                _inputStream = inputStream;
                _randomAccessStream = randomAccessStream;
                _offset = offset;
                _count = count;
                _leaveOpen = leaveOpen;
                _progressAction = progressAction;
                _buffer = new byte[64 * 1024];
                _bufferCount = 0;
                _bufferIndex = 0;
                _index = 0;
                if (offset.HasValue)
                    _randomAccessStream.Seek(offset.Value);
            }

            public byte Current
            {
                get
                {
                    // 既にオブジェクトが破棄されていれば例外
                    if (_isDisposed)
                        throw new ObjectDisposedException(GetType().FullName.ToString());
                    // 既にEOSに達していれば例外
                    if (_count.HasValue && _count.Value.CompareTo(_index) < 0)
                        throw new InvalidOperationException();
                    if (_bufferIndex >= _bufferCount)
                        throw new InvalidOperationException();

                    // 現在指しているデータを返す
                    return _buffer[_bufferIndex];
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                // 既にオブジェクトが破棄されていれば例外
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName.ToString());

                // index を一つ進める
                ++_bufferIndex;
                ++_index;
                if (_count.HasValue && _count.Value.CompareTo(_index) < 0)
                {
                    // 指定された回数だけ繰り返し終わった場合
                    return false;
                }
                if (_bufferIndex >= _bufferCount)
                {
                    // _buffer のデータの最後に到達した場合
                    // _sourceStream から新たなデータを読み込もうと試みる
                    _bufferCount = _inputStream.Read(_buffer, 0, _buffer.Length);
                    if (_bufferCount <= 0)
                    {
                        // _sourceStream の終端に達してしまった場合
                        return false;
                    }
                    _bufferIndex = 0;
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
#if DEBUG
                if (_bufferIndex >= _bufferCount)
                    throw new Exception();
#endif
                return true;
            }

            public void Reset()
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
                if (_randomAccessStream == null)
                    throw new NotSupportedException();

                _bufferCount = 0;
                _bufferIndex = 0;
                _index = 0;
                if (_offset.HasValue)
                    _randomAccessStream.Seek(_offset.Value);
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

        private IInputByteStream<POSITION_T> _baseStream;
        private IRandomInputByteStream<POSITION_T> _randomAccessStream;
        private POSITION_T? _offset;
        private UInt64? _count;
        private bool _leaveOpen;
        private Action<int> _progressAction;

        public ByteSequenceByByteStreamEnumerable(IInputByteStream<POSITION_T> baseStream, POSITION_T? offset, UInt64? count, bool leaveOpen, Action<int> progressAction)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                _baseStream = baseStream;
                _randomAccessStream = baseStream as IRandomInputByteStream<POSITION_T>;
                _offset = offset;
                _count = count;
                _leaveOpen = leaveOpen;
                _progressAction = progressAction;

                if (_offset.HasValue && _randomAccessStream == null)
                    throw new ArgumentException();
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public IEnumerator<byte> GetEnumerator() => new Enumerator(_baseStream, _randomAccessStream, _offset, _count, _leaveOpen, _progressAction);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
