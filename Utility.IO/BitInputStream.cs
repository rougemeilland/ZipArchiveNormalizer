using System;
using System.IO;

namespace Utility.IO
{
    public class BitInputStream
        : IDisposable
    {
        private bool _isDisosed;
        private Stream _baseStream;
        private bool _leaveOpen;
        private SerializedBitArray _buffer;
        private bool _isEndOfBaseStream;
        private bool _isEndOfStream;

        public BitInputStream(Stream baseStream, bool leaveOpen = false)
            : this(baseStream, BitPackingDirection.MsbToLsb, leaveOpen)
        {
        }

        public BitInputStream(Stream baseStream, BitPackingDirection packingDirection, bool leaveOpen = false)
        {
            if (baseStream == null)
                throw new ArgumentNullException("baseStream");
            if (baseStream.CanRead == false)
                throw new ArgumentException("'baseStream' is not suppot 'Read'.", "baseStream");

            _isDisosed = false;
            _baseStream = baseStream;
            PackingDirection = packingDirection;
            _leaveOpen = leaveOpen;
            _buffer = new SerializedBitArray();
            _isEndOfBaseStream = false;
            _isEndOfStream = false;
        }

        public BitPackingDirection PackingDirection { get; }

        public ReadOnlySerializedBitArray Read(int count)
        {
            if (count < 0)
                throw new ArgumentException();
            if (_isEndOfStream)
                return null;

            while (_isEndOfBaseStream == false && _buffer.Length < count)
            {
                var data = _baseStream.ReadByte();
                if (data < 0)
                {
                    _isEndOfBaseStream = true;
                    break;
                }
                _buffer.Append((Byte)data, packingDirection: PackingDirection);
            }
            if (_buffer.Length <= 0)
            {
                _isEndOfStream = true;
                return null;
            }
            return _buffer.Divide(count.Minimum(_buffer.Length), out _buffer).AsReadOnly();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
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
                _isDisosed = true;
            }
        }
    }
}
