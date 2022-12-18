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
            private readonly IEnumerable<byte> _source;

            private bool _isDisposed;
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

                    return _currentValue ?? throw new InvalidOperationException();
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_endOfStream)
                    return false;
                if (!_sourceEnumerator.MoveNext())
                {
                    _endOfStream = true;
                    return false;
                }
                var data1 = _sourceEnumerator.Current;
                if (ShiftJisChar.IsSingleByteChar(data1))
                {
                    // 1バイト文字
                    _currentValue = new ShiftJisChar(data1);
                    return true;
                }
                else
                {
                    // 2バイト文字
                    if (!_sourceEnumerator.MoveNext())
                        throw new UnexpectedEndOfSequenceException();
                    var data2 = _sourceEnumerator.Current;
                    _currentValue = new ShiftJisChar(data1, data2);
                    return true;
                }
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
                        _sourceEnumerator.Dispose();
                    _isDisposed = true;
                }
            }
        }

        private readonly IEnumerable<byte> _source;

        public ShiftJIsCharSequenceFromByteSequenceEnumerable(IEnumerable<byte> source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            _source = source;
        }

        public IEnumerator<ShiftJisChar> GetEnumerator() => new Enumerator(_source);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
