using System;
using System.IO;
using System.Linq;

namespace Utility.IO
{
    public class BitOutputStream
        : IDisposable
    {
        private bool _isDisosed;
        private Stream _baseStream;
        private bool _leaveOpen;
        private SerializedBitArray _buffer;


        public BitOutputStream(Stream baseStream, bool leaveOpen = false)
            : this(baseStream, BitPackingDirection.MsbToLsb, leaveOpen)
        {
        }

        public BitOutputStream(Stream baseStream, BitPackingDirection packingDirection, bool leaveOpen = false)
        {
            if (baseStream == null)
                throw new ArgumentNullException("baseStream");
            if (baseStream.CanWrite == false)
                throw new ArgumentException("'baseStream' is not suppot 'Write'.", "baseStream");

            _isDisosed = false;
            _baseStream = baseStream;
            PackingDirection = packingDirection;
            _leaveOpen = leaveOpen;
            _buffer = new SerializedBitArray();
        }

        public BitPackingDirection PackingDirection { get; }


        public void Write(ReadOnlySerializedBitArray bitArray)
        {
            if (bitArray == null)
                throw new ArgumentNullException("bitArray");

            _buffer.Append(bitArray);
            if (_buffer.Length > 8)
                _baseStream.Write(_buffer.Divide(_buffer.Length / 8 * 8, out _buffer).ToByteArray(PackingDirection));
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
#if DEBUG
                        if (_buffer.Length.IsAnyOf(0, 7) == false)
                            throw new Exception();
#endif
                        if (_buffer.Length > 0)
                        {
                            _buffer.Append(Enumerable.Repeat(false, 8 - _buffer.Length));
#if DEBUG
                            if (_buffer.Length != 8)
                                throw new Exception();
#endif
                            _baseStream.Write(_buffer.ToByteArray(PackingDirection));
                        }
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
