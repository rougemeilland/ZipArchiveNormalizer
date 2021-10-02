using System;
using System.IO;

namespace Utility.IO
{
    public abstract class PartialOutputStream<POSITION_T, BASE_POSITION_T>
        : IOutputByteStream<POSITION_T>
    {
        private bool _isDisposed;
        private UInt64? _size;
        private bool _leaveOpen;
        private IOutputByteStream<BASE_POSITION_T> _baseStream;
        private UInt64 _position;

        /// <summary>
        /// 元になるバイトストリームを使用して初期化するコンストラクタです。
        /// </summary>
        /// <param name="baseStream">
        /// 元になるバイトストリームを示す<see cref="IInputByteStream{BASE_POSITION_T}"/>オブジェクトです。
        /// </param>
        /// <param name="leaveOpen">
        /// コンストラクタによって初期化されたオブジェクトが破棄されるときに元になるバイトストリームもともに破棄するかどうかを示す<see cref="bool"/>値です。
        /// true の場合は元のバイトストリームを破棄しません。
        /// false の場合は元のバイトストリームを破棄します。
        /// </param>
        /// <remarks>
        /// このコンストラクタを使用した場合、元になったバイトストリーム上でアクセス可能な開始位置は、コンストラクタ呼び出し時点での<code>baseStream.Position</code>となり、
        /// アクセス可能な長さの制限はありません。
        /// </remarks>
        public PartialOutputStream(IOutputByteStream<BASE_POSITION_T> baseStream, bool leaveOpen)
            : this(baseStream, null, leaveOpen)
        {
        }

        /// <summary>
        /// 元になるバイトストリームとアクセス可能な範囲(長さ)を使用して初期化するコンストラクタです。
        /// </summary>
        /// <param name="baseStream">
        /// 元になるバイトストリームを示す<see cref="IInputByteStream{BASE_POSITION_T}"/>オブジェクトです。
        /// </param>
        /// <param name="size">
        /// 元になるバイトストリームで、現在位置からアクセス可能なバイト数を示す<see cref="ulong"/>値です。
        /// </param>
        /// <param name="leaveOpen">
        /// コンストラクタによって初期化されたオブジェクトが破棄されるときに元になるバイトストリームもともに破棄するかどうかを示す<see cref="bool"/>値です。
        /// true の場合は元のバイトストリームを破棄しません。
        /// false の場合は元のバイトストリームを破棄します。
        /// </param>
        /// <remarks>
        /// このコンストラクタを使用した場合、元になったバイトストリーム上でアクセス可能な開始位置は、コンストラクタ呼び出し時点での<code>baseStream.Position</code>となります。
        /// </remarks>
        public PartialOutputStream(IOutputByteStream<BASE_POSITION_T> baseStream, ulong size, bool leaveOpen)
            : this(baseStream, (ulong?)size, leaveOpen)
        {
        }

        /// <summary>
        /// 元になるバイトストリームとアクセス可能な範囲(開始位置と長さ)を使用して初期化するコンストラクタです。
        /// </summary>
        /// <param name="baseStream">
        /// 元になるバイトストリームを示す<see cref="IInputByteStream{BASE_POSITION_T}"/>オブジェクトです。
        /// </param>
        /// <param name="size">
        /// 元になるバイトストリームで、現在位置からアクセス可能な長さのバイト数を示す<see cref="ulong?"/>値です。
        /// nullの場合は、アクセス可能な長さの制限はありません。
        /// </param>
        /// <param name="leaveOpen">
        /// コンストラクタによって初期化されたオブジェクトが破棄されるときに元になるバイトストリームもともに破棄するかどうかを示す<see cref="bool"/>値です。
        /// true の場合は元のバイトストリームを破棄しません。
        /// false の場合は元のバイトストリームを破棄します。
        /// </param>
        public PartialOutputStream(IOutputByteStream<BASE_POSITION_T> baseStream, ulong? size, bool leaveOpen)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                _isDisposed = false;
                _baseStream = baseStream;
                _size = size;
                _leaveOpen = leaveOpen;
                _position = 0;
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public POSITION_T Position
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                return AddPosition(ZeroPositionValue, _position);
            }
        }

        public int Write(IReadOnlyArray<byte> buffer, int offset, int count)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentException("'offset' must not be negative.", nameof(offset));
            if (count < 0)
                throw new ArgumentException("'count' must not be negative.", nameof(count));
            if (offset + count > buffer.Length)
                throw new IndexOutOfRangeException("'offset + count' is greater than 'buffer.Length'.");

            var actualCount = count;
            if (_size.HasValue)
            {
                actualCount =
                    _size.Value > _position
                    ? (int)((ulong)actualCount).Minimum(_size.Value - _position)
                    : 0;
            }
            if (count > 0 && actualCount <= 0)
                throw new IOException("Can not write any more.");


            var written = _baseStream.Write(buffer, offset, actualCount);
            _position += (uint)written;
            return written;
        }

        public void Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            _baseStream.Flush();
        }

        public void Close()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            Dispose();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected abstract POSITION_T ZeroPositionValue { get; }
        protected abstract POSITION_T AddPosition(POSITION_T x, UInt64 y);

        protected void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_baseStream != null)
                    {
                        try
                        {
                            _baseStream.Flush();
                        }
                        catch (Exception)
                        {
                        }
                        if (_leaveOpen == false)
                            _baseStream.Dispose();
                        _baseStream = null;
                    }
                }
                _isDisposed = true;
            }
        }
    }
}