using System;
using System.Collections.Generic;

namespace Utility.IO
{
    class SequentialInputBitStreamBySequence
        : SequentialInputBitStreamBy
    {
        private bool _isDisosed;
        private IEnumerator<byte> _sourceSequenceEnumerator;

        public SequentialInputBitStreamBySequence(IEnumerable<byte> sourceSequence, BitPackingDirection packingDirection)
            : base(packingDirection)
        {
            if (sourceSequence == null)
                throw new ArgumentNullException(nameof(sourceSequence));

            _isDisosed = false;
            _sourceSequenceEnumerator = sourceSequence.GetEnumerator();
        }

        protected override byte? GetNextByte()
        {
            if (_sourceSequenceEnumerator.MoveNext())
                return _sourceSequenceEnumerator.Current;
            else
                return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisosed)
            {
                if (disposing)
                {
                    if (_sourceSequenceEnumerator != null)
                    {
                        _sourceSequenceEnumerator.Dispose();
                        _sourceSequenceEnumerator = null;
                    }
                }
                base.Dispose();
                _isDisosed = true;
            }
        }
    }
}
