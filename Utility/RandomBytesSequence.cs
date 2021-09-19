using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Utility
{
    class RandomBytesSequence
        : IEnumerable<IReadOnlyArray<byte>>
    {
        private class Enumerator
            : IEnumerator<IReadOnlyArray<byte>>
        {
            private bool _isDisposed;
            private int _byteLength;
            private RandomNumberGenerator _randomValueGenerator;
            private IReadOnlyArray<byte> _value;

            public Enumerator(int byteLength)
            {
                _isDisposed = false;
                _byteLength = byteLength;
                _randomValueGenerator = new RNGCryptoServiceProvider();
            }

            public IReadOnlyArray<byte> Current => _value;

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                var buffer = new byte[_byteLength];
                _randomValueGenerator.GetBytes(buffer);
                _value = buffer.AsReadOnly();
                return true;
            }

            public void Reset()
            {
                // ランダムな値を返し続けて終わらないシーケンスなので Reset は意味がない
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_isDisposed)
                {
                    if (disposing)
                    {
                        if (_randomValueGenerator != null)
                        {
                            _randomValueGenerator.Dispose();
                            _randomValueGenerator = null;
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

        private int _byteLength;

        public RandomBytesSequence(int byteLength)
        {
            _byteLength = byteLength;
        }

        public IEnumerator<IReadOnlyArray<byte>> GetEnumerator()
        {
            return new Enumerator(_byteLength);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
