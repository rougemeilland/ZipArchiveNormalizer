using System;
using System.Collections;
using System.Collections.Generic;

namespace Utility
{
    internal abstract class CrcCalculationMethod<CRC_VALUE_T>
        where CRC_VALUE_T : struct
    {
        private class PathThroughSequenceWithCrcCalculationEnumerable
            : IEnumerable<byte>
        {
            private class Enumerator
                : IEnumerator<byte>
            {
                private bool _isDisposed;
                private IEnumerable<byte> _source;
                private CrcCalculationMethod<CRC_VALUE_T> _crcCalculator;
                private ValueHolder<CRC_VALUE_T> _result;
                private IEnumerator<byte> _sourceEnumerator;
                private CRC_VALUE_T _crc;

                public Enumerator(IEnumerable<byte> source, CrcCalculationMethod<CRC_VALUE_T> crcCalculator, ValueHolder<CRC_VALUE_T> result)
                {
                    _source = source;
                    _crcCalculator = crcCalculator;
                    _result = result;
                    _sourceEnumerator = _source.GetEnumerator();
                    _crc = _crcCalculator.InitialValue;
                    _result.Value = default(CRC_VALUE_T);
                }

                public byte Current => _sourceEnumerator.Current;
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if (_sourceEnumerator.MoveNext())
                    {
                        _crc = _crcCalculator.Update(_crc, _sourceEnumerator.Current);
                        return true;
                    }
                    else
                    {
                        _crc = _crcCalculator.Finalize(_crc);
                        _result.Value = _crc;
                        return false;
                    }
                }

                public void Reset()
                {
                    _sourceEnumerator?.Dispose();
                    _sourceEnumerator = _source.GetEnumerator();
                    _crc = _crcCalculator.InitialValue;
                    _result.Value = default(CRC_VALUE_T);
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

                public void Dispose()
                {
                    Dispose(disposing: true);
                    GC.SuppressFinalize(this);
                }
            }

            private IEnumerable<byte> _source;
            private CrcCalculationMethod<CRC_VALUE_T> _crcCalculator;
            private ValueHolder<CRC_VALUE_T> _result;

            public PathThroughSequenceWithCrcCalculationEnumerable(IEnumerable<byte> source, CrcCalculationMethod<CRC_VALUE_T> crcCalculator, ValueHolder<CRC_VALUE_T> result)
            {
                _source = source;
                _crcCalculator = crcCalculator;
                _result = result;
            }

            public IEnumerator<byte> GetEnumerator() => new Enumerator(_source, _crcCalculator, _result);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public CRC_VALUE_T Calculate(IEnumerable<byte> dataSequence)
        {
            var crc = InitialValue;
            foreach (var data in dataSequence)
                crc = Update(crc, data);
            return Finalize(crc);
        }

        public CRC_VALUE_T Calculate(IEnumerable<byte> dataSequence, out ulong count)
        {
            count = 0;
            var crc = InitialValue;
            foreach (var data in dataSequence)
            {
                crc = Update(crc, data);
                ++count;
            }
            return Finalize(crc);
        }

        public IEnumerable<byte> GetSequenceWithCrc(IEnumerable<byte> source, ValueHolder<CRC_VALUE_T> result) =>
                new PathThroughSequenceWithCrcCalculationEnumerable(source, this, result);

        public CRC_VALUE_T Calculate(IReadOnlyArray<byte> array) => Calculate(array, 0, array.Length);

        public CRC_VALUE_T Calculate(IReadOnlyArray<byte> array, int offset, int count)
        {
            var crc = InitialValue;
            for (var index = 0; index < count; ++count)
                crc = Update(crc, array[offset + index]);
            return Finalize(crc);
        }

        protected abstract CRC_VALUE_T InitialValue { get; }
        protected abstract CRC_VALUE_T Update(CRC_VALUE_T crc, byte data);
        protected abstract CRC_VALUE_T Finalize(CRC_VALUE_T crc);
    }
}
