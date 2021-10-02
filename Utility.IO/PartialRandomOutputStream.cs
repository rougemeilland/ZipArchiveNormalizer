using System;
using System.IO;

namespace Utility.IO
{
    public abstract class PartialRandomOutputStream<POSITION_T, BASE_POSITION_T>
        : IRandomOutputByteStream<POSITION_T>
        where BASE_POSITION_T : struct, IComparable<BASE_POSITION_T>, IEquatable<BASE_POSITION_T>
    {
        private bool _isDisposed;
        private IRandomOutputByteStream<BASE_POSITION_T> _baseStream;
        private BASE_POSITION_T _startOfStream;
        private BASE_POSITION_T? _limitOfStream;
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
        /// アクセス可能な長さの制限はありません。
        /// </remarks>
        public PartialRandomOutputStream(IRandomOutputByteStream<BASE_POSITION_T> baseStream, bool leaveOpen)
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
        public PartialRandomOutputStream(IRandomOutputByteStream<BASE_POSITION_T> baseStream, ulong size, bool leaveOpen)
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
        public PartialRandomOutputStream(IRandomOutputByteStream<BASE_POSITION_T> baseStream, BASE_POSITION_T offset, ulong size, bool leaveOpen)
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
        /// 元になるバイトストリームで、<paramref name="offset"/>で与えられた位置からアクセス可能な長さのバイト数を示す<see cref="ulong?"/>値です。
        /// nullの場合は、アクセス可能な長さの制限はありません。
        /// </param>
        /// <param name="leaveOpen">
        /// コンストラクタによって初期化されたオブジェクトが破棄されるときに元になるバイトストリームもともに破棄するかどうかを示す<see cref="bool"/>値です。
        /// true の場合は元のバイトストリームを破棄しません。
        /// false の場合は元のバイトストリームを破棄します。
        /// </param>
        public PartialRandomOutputStream(IRandomOutputByteStream<BASE_POSITION_T> baseStream, BASE_POSITION_T? offset, ulong? size, bool leaveOpen)
        {
            try
            {
                if (baseStream == null)
                    throw new ArgumentNullException(nameof(baseStream));

                _isDisposed = false;
                _baseStream = baseStream;
                _startOfStream = offset ?? _baseStream.Position;
                _limitOfStream = size.HasValue ? AddBasePosition(_startOfStream, size.Value) : (BASE_POSITION_T?)null;
                _leaveOpen = leaveOpen;

                if (!_baseStream.Position.Equals(_startOfStream))
                    _baseStream.Seek(_startOfStream);
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

                checked
                {
                    var endOfStream = AddBasePosition(ZeroBasePositionValue, _baseStream.Length);
                    if (_limitOfStream.HasValue)
                        endOfStream = endOfStream.Minimum(_limitOfStream.Value);
                    return GetDistanceBetweenBasePositions(endOfStream, _startOfStream);
                }
            }

            set
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
                if (value < 0)
                    throw new ArgumentException();

                checked
                {
                    _baseStream.Length = value + GetDistanceBetweenBasePositions(_startOfStream, ZeroBasePositionValue);
                }
            }
        }

        public POSITION_T Position
        {
            get
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                checked
                {
                    if (_baseStream.Position.CompareTo(_startOfStream) < 0)
                        throw new IOException();

                    return AddPosition(ZeroPositionValue, GetDistanceBetweenBasePositions(_baseStream.Position, _startOfStream));
                }
            }
        }

        public void Seek(POSITION_T offset)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            _baseStream.Seek(AddBasePosition(_startOfStream, GetDistanceBetweenPositions(offset, ZeroPositionValue)));
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
            if (_baseStream.Position.CompareTo(_startOfStream) < 0)
                throw new IOException();

            var actualCount = count;
            if (_limitOfStream.HasValue)
                actualCount = (int)(GetDistanceBetweenBasePositions(_limitOfStream.Value, _baseStream.Position).Minimum((uint)actualCount));
            if (count > 0 && actualCount <= 0)
                throw new IOException("Can not write any more.");
            return _baseStream.Write(buffer, offset, count);
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
        protected abstract BASE_POSITION_T ZeroBasePositionValue { get; }
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