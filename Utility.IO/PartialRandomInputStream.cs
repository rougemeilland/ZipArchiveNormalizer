using System;
using System.IO;

namespace Utility.IO
{
    /// <summary>
    /// バイトストリームの部分的な範囲のアクセスのみを可能にするクラスです。
    /// </summary>
    public abstract class PartialRandomInputStream<POSITION_T, BASE_POSITION_T>
        : IRandomInputByteStream<POSITION_T>
        where BASE_POSITION_T: struct, IComparable<BASE_POSITION_T>, IEquatable<BASE_POSITION_T>
    {
        private bool _isDisposed;
        private BASE_POSITION_T _startOfStream;
        private BASE_POSITION_T _endOfStream;
        private bool _leaveOpen;

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
        /// 元になったバイトストリームの終端までアクセス可能になります。
        /// </remarks>
        public PartialRandomInputStream(IRandomInputByteStream<BASE_POSITION_T> baseStream, bool leaveOpen)
            : this(baseStream, null, null, leaveOpen)
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
        public PartialRandomInputStream(IRandomInputByteStream<BASE_POSITION_T> baseStream, ulong size, bool leaveOpen)
            : this(baseStream, null, size, leaveOpen)
        {
        }

        /// <summary>
        /// 元になるバイトストリームとアクセス可能な範囲(開始位置と長さ)を使用して初期化するコンストラクタです。
        /// </summary>
        /// <param name="baseStream">
        /// 元になるバイトストリームを示す<see cref="IInputByteStream{BASE_POSITION_T}"/>オブジェクトです。
        /// </param>
        /// <param name="offset">
        /// 元になるバイトストリームで、アクセスが許可される最初の位置を示す<see cref="BASE_POSITION_T"/>値です。
        /// </param>
        /// <param name="size">
        /// 元になるバイトストリームで、<paramref name="offset"/>で与えられた位置からアクセス可能なバイト数を示す<see cref="ulong"/>値です。
        /// </param>
        /// <param name="leaveOpen">
        /// コンストラクタによって初期化されたオブジェクトが破棄されるときに元になるバイトストリームもともに破棄するかどうかを示す<see cref="bool"/>値です。
        /// true の場合は元のバイトストリームを破棄しません。
        /// false の場合は元のバイトストリームを破棄します。
        /// </param>
        public PartialRandomInputStream(IRandomInputByteStream<BASE_POSITION_T> baseStream, BASE_POSITION_T offset, ulong size, bool leaveOpen)
            : this(baseStream, (BASE_POSITION_T?)offset, (ulong?)size, leaveOpen)
        {
        }

        /// <summary>
        /// 元になるバイトストリームとアクセス可能な範囲(開始位置と長さ)を使用して初期化するコンストラクタです。
        /// </summary>
        /// <param name="baseStream">
        /// 元になるバイトストリームを示す<see cref="IInputByteStream{BASE_POSITION_T}"/>オブジェクトです。
        /// </param>
        /// <param name="offset">
        /// 元になるバイトストリームで、アクセスが許可される最初の位置を示す<see cref="BASE_POSITION_T?"/>値です。
        /// nullの場合は、元になるバイトストリームの現在位置<code>baseStream.Position</code>が最初の位置とみなされます。
        /// </param>
        /// <param name="size">
        /// 元になるバイトストリームで、<paramref name="offset"/>で与えられた位置からアクセス可能なバイト数を示す<see cref="ulong?"/>値です。
        /// nullの場合は、元になるバイトストリームの終端までアクセスが可能になります。
        /// </param>
        /// <param name="leaveOpen">
        /// コンストラクタによって初期化されたオブジェクトが破棄されるときに元になるバイトストリームもともに破棄するかどうかを示す<see cref="bool"/>値です。
        /// true の場合は元のバイトストリームを破棄しません。
        /// false の場合は元のバイトストリームを破棄します。
        /// </param>
        public PartialRandomInputStream(IRandomInputByteStream<BASE_POSITION_T> baseStream, BASE_POSITION_T? offset, ulong? size, bool leaveOpen)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                _isDisposed = false;
                BaseStream = baseStream;
                _startOfStream = offset ?? BaseStream.Position;
                _endOfStream = size.HasValue ? AddBasePosition(_startOfStream, size.Value) : EndBasePositionValue;
                _leaveOpen = leaveOpen;

                if (_startOfStream.CompareTo(EndBasePositionValue) > 0)
                    throw new ArgumentException();

                if (!BaseStream.Position.Equals(_startOfStream))
                    BaseStream.Seek(_startOfStream);
            }
            catch (Exception)
            {
                if (leaveOpen == false)
                    baseStream?.Dispose();
                throw;
            }
        }

        public ulong Length
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                return GetDistanceBetweenBasePositions(_endOfStream, _startOfStream);
            }

            set => throw new NotSupportedException();
        }

        public POSITION_T Position
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
                if (BaseStream.Position.CompareTo(_startOfStream) < 0)
                    throw new IOException();

                checked
                {
                    return AddPosition(ZeroPositionValue, GetDistanceBetweenBasePositions(BaseStream.Position, _startOfStream));
                }
            }
        }

        public void Seek(POSITION_T offset)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            checked
            {
                BaseStream.Seek(AddBasePosition(_startOfStream, GetDistanceBetweenPositions(offset, ZeroPositionValue)));
            }
        }

        public int Read(byte[] buffer, int offset, int count)
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

            if (BaseStream.Position.CompareTo(_startOfStream) < 0)
                throw new IOException();

            var actualCount = GetDistanceBetweenBasePositions(_endOfStream, BaseStream.Position).Minimum((uint)count);
            if (actualCount <= 0)
                return 0;
            var length = BaseStream.Read(buffer, offset, (int)actualCount);
            if (actualCount > 0 && length <= 0)
                throw new IOException("Stream length is not match");
            return length;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected IRandomInputByteStream<BASE_POSITION_T> BaseStream { get; private set; }
        protected abstract POSITION_T ZeroPositionValue { get; }
        protected abstract BASE_POSITION_T EndBasePositionValue { get; }
        protected abstract UInt64 GetDistanceBetweenPositions(POSITION_T x, POSITION_T y);
        protected abstract UInt64 GetDistanceBetweenBasePositions(BASE_POSITION_T x, BASE_POSITION_T y);
        protected abstract POSITION_T AddPosition(POSITION_T x, UInt64 y);
        protected abstract BASE_POSITION_T AddBasePosition(BASE_POSITION_T x, UInt64 y);

        protected void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (BaseStream != null)
                    {
                        if (_leaveOpen == false)
                            BaseStream.Dispose();
                        BaseStream = null;
                    }
                }
                _isDisposed = true;
            }
        }
    }
}
