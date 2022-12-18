﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Utility.IO
{
    class SequentialInputBitStreamBySequence
        : SequentialInputBitStreamBy
    {
        private readonly IEnumerator<byte> _sourceSequenceEnumerator;

        private bool _isDisposed;

        public SequentialInputBitStreamBySequence(IEnumerable<byte> sourceSequence, BitPackingDirection bitPackingDirection)
            : base(bitPackingDirection)
        {
            if (sourceSequence is null)
                throw new ArgumentNullException(nameof(sourceSequence));

            _isDisposed = false;
            _sourceSequenceEnumerator = sourceSequence.GetEnumerator();
        }

        protected override byte? GetNextByte() =>
            _sourceSequenceEnumerator.MoveNext() ? _sourceSequenceEnumerator.Current : null;

        protected override Task<byte?> GetNextByteAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_sourceSequenceEnumerator.MoveNext() ? _sourceSequenceEnumerator.Current : (byte?)null);

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                    _sourceSequenceEnumerator.Dispose();
                _isDisposed = true;
            }
            base.Dispose(disposing);
        }

        protected async override ValueTask DisposeAsyncCore()
        {
            if (!_isDisposed)
            {
                _sourceSequenceEnumerator.Dispose();
                _isDisposed = true;
            }
            await base.DisposeAsyncCore().ConfigureAwait(false);
        }
    }
}
