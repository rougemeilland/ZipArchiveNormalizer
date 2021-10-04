using System;
using System.Collections;
using System.Collections.Generic;

namespace Utility.Text
{
    class ShiftJIsCharSequenceFromByteSequenceEnumerable
        : IEnumerable<ShiftJisChar>
    {
        private class Enumerator
            : IEnumerator<ShiftJisChar>
        {
            private bool _isDisposed;
            private IEnumerable<byte> _source;
            private IEnumerator<byte> _sourceEnumerator;
            private ShiftJisChar? _currentValue;
            private bool _endOfStream;

            public Enumerator(IEnumerable<byte> source)
            {
                _isDisposed = false;
                _source = source;
                _sourceEnumerator = _source.GetEnumerator();
                _currentValue = null;
                _endOfStream = false;
            }

            public ShiftJisChar Current
            {
                get
                {
                    if (_isDisposed)
                        throw new ObjectDisposedException(GetType().FullName);
                    if (_endOfStream)
                        throw new InvalidOperationException();
                    if (_currentValue == null)
                        throw new InvalidOperationException();
                    return _currentValue.Value;
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_endOfStream)
                    return false;
                if (_sourceEnumerator.MoveNext() == false)
                {
                    _endOfStream = true;
                    return false;
                }
                var data1 = _sourceEnumerator.Current;
                if (data1.IsBetween((byte)0x00, (byte)0x80) ||
                    data1.IsBetween((byte)0xa0, (byte)0xdf) ||
                    data1.IsBetween((byte)0xfd, (byte)0xff))
                {
                    // 1バイト文字
                    _currentValue = new ShiftJisChar(data1);
                    return true;
                }
                if (_sourceEnumerator.MoveNext() == false)
                    throw new UnexpectedEndOfSequenceException();
                var data2 = _sourceEnumerator.Current;
                if (data2.IsBetween((byte)0x40, (byte)0x7e) ||
                    data2.IsBetween((byte)0x80, (byte)0xfc))
                {
                    // 2バイト文字
                    _currentValue = new ShiftJisChar(data1, data2);
                    return true;
                }
                throw new BadShiftJisEncodingException();
            }

            public void Reset()
            {
                _sourceEnumerator?.Dispose();
                _sourceEnumerator = _source.GetEnumerator();
                _currentValue = null;
                _endOfStream = false;
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
                        if (_sourceEnumerator != null)
                        {
                            _sourceEnumerator.Dispose();
                            _sourceEnumerator = null;
                        }
                    }
                    _isDisposed = true;
                }
            }
        }

        private IEnumerable<byte> _source;

        public ShiftJIsCharSequenceFromByteSequenceEnumerable(IEnumerable<byte> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            _source = source;
        }

        public IEnumerator<ShiftJisChar> GetEnumerator() => new Enumerator(_source);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
