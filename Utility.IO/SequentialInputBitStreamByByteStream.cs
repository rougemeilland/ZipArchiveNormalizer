using System;

namespace Utility.IO
{
    class SequentialInputBitStreamByByteStream
        : SequentialInputBitStreamBy
    {
        private bool _isDisosed;
        private IInputByteStream<UInt64> _baseStream;
        private bool _leaveOpen;
        private byte[] _buffer;

        public SequentialInputBitStreamByByteStream(IInputByteStream<UInt64> baseStream, BitPackingDirection packingDirection, bool leaveOpen)
            : base(packingDirection)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                _isDisosed = false;
                _baseStream = baseStream;
                _leaveOpen = leaveOpen;
                _buffer = new byte[1];
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        protected override byte? GetNextByte()
        {
            var length = _baseStream.Read(_buffer, 0, 1);
            if (length <= 0)
                return null;
            else
                return _buffer[0];
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisosed)
            {
                if (disposing)
                {
                    if (_baseStream != null)
                    {
                        if (_leaveOpen == false)
                            _baseStream.Dispose();
                        _baseStream = null;
                    }
                }
                base.Dispose();
                _isDisosed = true;
            }
        }
    }
}
