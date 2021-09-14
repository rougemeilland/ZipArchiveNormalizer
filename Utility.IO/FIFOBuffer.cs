using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Utility.IO
{
    public class FifoBuffer
        : IFifoReadable, IFifoWritable
    {
        private class InputStream
            : Stream
        {
            private bool _isDisposed;
            private IFifoReadable _reader;
            private long _totalCount;

            public InputStream(IFifoReadable reader)
            {
                _isDisposed = false;
                _reader = reader;
                _totalCount = 0;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
                if (buffer == null)
                    throw new ArgumentNullException();
                if (offset < 0)
                    throw new ArgumentException();
                if (count < 0)
                    throw new ArgumentException();

                var length = _reader.Read(buffer, offset, count);
                if (length > 0)
                {
                    _totalCount += length;
                    _reader.SetReadCount(_totalCount);
                }
                return length;
            }

            public override void Flush()
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);
            }

            protected override void Dispose(bool disposing)
            {
                if (!_isDisposed)
                {
                    if (disposing)
                    {
                        if (_reader != null)
                        {
                            _reader.Close();
                            _reader = null;
                        }
                    }
                    _isDisposed = true;
                    base.Dispose(disposing);
                }
            }

            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
        }

        private class OutputStream
            : Stream
        {
            private bool _isDisposed;
            private IFifoWritable _writer;
            private bool _synchronouslyWriting;
            private long _totalCount;
            private CancellationTokenSource _cts;

            public OutputStream(IFifoWritable writer, bool synchronouslyWriting)
            {
                _isDisposed = false;
                _writer = writer;
                _synchronouslyWriting = synchronouslyWriting;
                _totalCount = 0;
                _cts = new CancellationTokenSource();
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;


            public override void Write(byte[] buffer, int offset, int count)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                _writer.Write(buffer, offset, count);
                _totalCount += count;
                if (_synchronouslyWriting)
                    InternalFlush();
            }

            public override void Flush()
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(GetType().FullName);

                InternalFlush();
            }

            protected override void Dispose(bool disposing)
            {
                if (!_isDisposed)
                {
                    if (disposing)
                    {
                        if (_writer != null)
                        {
                            _writer.Close();
                            _writer = null;
                        }
                        if (_cts != null)
                        {
                            _cts.Dispose();
                            _cts = null;
                        }
                    }
                    _isDisposed = true;
                    base.Dispose(disposing);
                }
            }

            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();

            private void InternalFlush()
            {
                try
                {
                    _writer.WaitForReadCount(_totalCount, _cts.Token);
                }
                catch (ObjectDisposedException ex)
                {
                    throw new IOException("The stream is closed.", ex);
                }
                catch (OperationCanceledException ex)
                {
                    throw new IOException("The stream is closed.", ex);
                }
            }

        }

        private class FifoQueue
        {
            private int _currentCount;
            private LinkedList<byte[]> _queue;

            public FifoQueue(int maximumBufferSize)
            {
                MaximumCount = maximumBufferSize;
                _currentCount = 0;
                _queue = new LinkedList<byte[]>();
            }

            public int Dequeue(byte[] buffer, int offset, int count)
            {
                lock (this)
                {
                    var firstNode = _queue.First;
                    if (firstNode == null)
                        return 0;
                    else if (firstNode.Value.Length <= count)
                    {
                        Array.Copy(firstNode.Value, 0, buffer, offset, firstNode.Value.Length);
                        _queue.RemoveFirst();
                        _currentCount -= firstNode.Value.Length;
                        return firstNode.Value.Length;
                    }
                    else
                    {
                        Array.Copy(firstNode.Value, 0, buffer, offset, count);
                        var newBuffer = new byte[firstNode.Value.Length - count];
                        Array.Copy(firstNode.Value, count, newBuffer, 0, newBuffer.Length);
                        firstNode.Value = newBuffer;
                        _currentCount -= count;
                        return count;
                    }
                }
            }

            public int Enqueue(byte[] buffer, int offset, int count)
            {
                lock (this)
                {
                    var actualCount = count;
                    if (_currentCount + actualCount > MaximumCount)
                        actualCount = MaximumCount - _currentCount;
                    if (actualCount > 0)
                    {
                        var newBuffer = new byte[actualCount];
                        Array.Copy(buffer, offset, newBuffer, 0, actualCount);
                        _queue.AddLast(newBuffer);
                        _currentCount += newBuffer.Length;
                    }
                    return actualCount;
                }
            }

            public bool IsEmpty
            {
                get
                {
                    lock (this)
                    {
                        return _currentCount <= 0;
                    }
                }
            }

            public bool IsFull
            {
                get
                {
                    lock (this)
                    {
                        return _currentCount >= MaximumCount;
                    }
                }
            }

            public int MaximumCount { get; }
        }

        private const int _maximumBufferSize = 81920;
        private bool _isDisposed;
        private FifoQueue _queue;
        private bool _isOpenedReader;
        private bool _isOpenedByWriter;
        private bool _isClosedByReader;
        private bool _isClosedByWriter;
        private ManualResetEventSlim _isReadyForReaderEvent;
        private ManualResetEventSlim _isReadyForWriterEvent;
        private ManualResetEventSlim _isReadyForReadCountEvent;
        private CancellationTokenSource _accessCanceller;
        private long _totalReadCount;
        private long _expectedReadCount;

        public FifoBuffer(int maximumBufferSize = _maximumBufferSize)
        {
            _isDisposed = false;
            _queue = new FifoQueue(maximumBufferSize);
            _isOpenedReader = false;
            _isOpenedByWriter = false;
            _isClosedByReader = false;
            _isClosedByWriter = false;
            _isReadyForReaderEvent = new ManualResetEventSlim();
            _isReadyForWriterEvent = new ManualResetEventSlim();
            _isReadyForReadCountEvent = new ManualResetEventSlim();
            _accessCanceller = new CancellationTokenSource();
            _totalReadCount = 0;
            _expectedReadCount = long.MaxValue;
        }

        public Stream GetInputStream()
        {
            lock (this)
            {
                if (_isOpenedReader)
                    throw new InvalidOperationException("Can open reader only once.");
                var stream = new InputStream(this);
                _isOpenedReader = true;
                return stream;
            }
        }

        public Stream GetOutputStream(bool synchronouslyWriting)
        {
            lock (this)
            {
                if (_isOpenedByWriter)
                    throw new InvalidOperationException("Can open writer only once.");
                var stream = new OutputStream(this, synchronouslyWriting);
                _isOpenedByWriter = true;
                return stream;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    if (_accessCanceller != null)
                    {
                        _accessCanceller.Cancel();
                        _accessCanceller.Dispose();
                        _accessCanceller = null;
                    }
                    if (_isReadyForReaderEvent != null)
                    {
                        _isReadyForReaderEvent.Dispose();
                        _isReadyForReaderEvent = null;
                    }
                    if (_isReadyForWriterEvent != null)
                    {
                        _isReadyForWriterEvent.Dispose();
                        _isReadyForWriterEvent = null;
                    }
                    if (_isReadyForReadCountEvent != null)
                    {
                        _isReadyForReadCountEvent.Dispose();
                        _isReadyForReadCountEvent = null;
                    }
                }
                _isDisposed = true;
            }
        }

        private int InternalRead(byte[] buffer, int offset, int count)
        {
            while (true)
            {
                if (_isDisposed)
                {
                    // このオブジェクトが Dispose されていることを検出した場合
                    // ストリームの強制的な close とみなして 0 を返す。
                    return 0;
                }

                // キューが空である間はイベント待機を続ける
                while (_queue.IsEmpty)
                {
                    lock (this)
                    {
                        if (_isClosedByReader)
                            throw new IOException("The stream is already closed by reader.");
                        if (_isClosedByWriter)
                            break;
                    }
                    try
                    {
                        _isReadyForReaderEvent.Wait(_accessCanceller.Token);
                    }
                    catch (ObjectDisposedException)
                    {
                        // 読み込み中にこのオブジェクトが Dispose された場合
                        // ストリームの強制的な close とみなして 0 を返す。
                        return 0;
                    }
                    catch (OperationCanceledException)
                    {
                        // 読み込み中にこのオブジェクトが Dispose された場合
                        // ストリームの強制的な close とみなして 0 を返す。
                        return 0;
                    }
                }
                lock (this)
                {
                    if (_isClosedByReader)
                        throw new IOException("The stream is already closed by reader.");
                    var length = _queue.Dequeue(buffer, offset, count);
                    if (length > 0)
                    {
                        // キューからデータが読み取れた場合

                        // キューにあるデータをバッファに格納できる範囲で読み込む
                        var totalCount = length;
                        while (totalCount < count && _queue.IsEmpty == false)
                        {
                            length = _queue.Dequeue(buffer, offset + totalCount, count - totalCount);
                            if (length <= 0)
                                throw new Exception("Dequeue cannot return 0 even though IsEmpty is false.");
                            totalCount += length;
                        }
                        SetQueueStatus();
                        return totalCount;
                    }

                    // queue にデータがない場合
                    if (_queue.IsEmpty == false)
                        throw new Exception("IsEmpty cannot be false even though the Dequeue is returning 0.");
                    if (_isClosedByWriter)
                    {
                        // writer から close されている場合
                        return 0;
                    }

                    // この時点で、キューにデータは存在しないが、 writer から close もされていない
                    // 次のデータが書き込まれるのを待つために処理を続行する
                }
            }
        }

        private int InternalWrite(byte[] buffer, int offset, int count)
        {
            while (true)
            {
                if (_isDisposed)
                {
                    // このオブジェクトが Dispose されていることを検出した場合
                    // ストリームの強制的な close とみなす。
                    throw new IOException("The stream is already closed.");
                }

                // キューが満杯である間はイベント待機を続ける
                while (_queue.IsFull)
                {
                    lock (this)
                    {
                        if (_isClosedByReader)
                            throw new IOException("The stream is already closed by reader.");
                        if (_isClosedByWriter)
                            throw new IOException("The stream is already closed by writer.");
                    }
                    try
                    {
                        _isReadyForWriterEvent.Wait(_accessCanceller.Token);
                    }
                    catch (ObjectDisposedException)
                    {
                        // 読み込み中にこのオブジェクトが Dispose された場合
                        // ストリームの強制的な close とみなす。
                        throw new IOException("The stream is already closed.");
                    }
                    catch (OperationCanceledException)
                    {
                        // 読み込み中にこのオブジェクトが Dispose された場合
                        // ストリームの強制的な close とみなす。
                        throw new IOException("The stream is already closed.");
                    }
                }
                lock (this)
                {
                    if (_isClosedByReader)
                        throw new IOException("The stream is already closed by reader.");
                    if (_isClosedByWriter)
                        throw new IOException("The stream is already closed by writer.");
                    var length = _queue.Enqueue(buffer, offset, count);
                    if (length > 0)
                    {
                        // キューにデータを追加できた場合
                        SetQueueStatus();
                        return length;
                    }

                    // queue にデータを追加できなかった場合
                    if (_queue.IsFull == false)
                        throw new Exception("IsFull cannot be false even though the Dequeue is returning 0.");

                    // この時点で、キューは満杯であるが、 reader からも writer からも close はされていない。
                    // 次にキューが空くのを待機するために処理を続行する
                }
            }
        }

        private void SetQueueStatus()
        {
            if (_isClosedByReader)
            {
                if (_isClosedByWriter)
                {
                    // reader と writer の両方から close されている場合、このオブジェクトはもう不要であるので Dispose する。
                    _isReadyForReaderEvent.Set();
                    _isReadyForWriterEvent.Set();
                    _isReadyForReadCountEvent.Set();
                    Dispose();
                }
                else
                {
                    // 少なくとも reader から close されている場合、 reader も writer も待機を続ける必要はないので、
                    // _isReadyForReaderEvent と _isReadyForWriterEvent の両方を Set する。
                    _isReadyForReaderEvent.Set();
                    _isReadyForWriterEvent.Set();
                    _isReadyForReadCountEvent.Set();
                }
            }
            else if (_isClosedByWriter)
            {
                // reader から close はされていないが writer から close されている場合
                // 少なくとも writer の待機は不要になるので、 _isReadyForWriterEvent は Set する。
                // reader は、以降 queue に残ったデータを読み取るしかないので、同じく待機は不要となるので、 _isReadyForReaderEvent も Set する。
                _isReadyForReaderEvent.Set();
                _isReadyForWriterEvent.Set();
                _isReadyForReadCountEvent.Set();
            }
            else
            {
                // reader からも writer からも close されていない場合、キューのデータの状況によりイベントを Set/Reset する。
                if (_queue.IsEmpty)
                {
                    _isReadyForReaderEvent.Reset();
                    _isReadyForReadCountEvent.Set();
                }
                else
                {
                    _isReadyForReaderEvent.Set();
                    _isReadyForReadCountEvent.Reset();
                }

                if (_queue.IsFull)
                    _isReadyForWriterEvent.Reset();
                else
                    _isReadyForWriterEvent.Set();

                if (_totalReadCount >= _expectedReadCount)
                    _isReadyForReadCountEvent.Set();
                else
                    _isReadyForReadCountEvent.Reset();
            }
        }

        private void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        int IFifoReadable.Read(byte[] buffer, int offset, int count)
        {
            return InternalRead(buffer, offset, count);
        }

        void IFifoReadable.SetReadCount(long count)
        {
            lock (this)
            {
                _totalReadCount = count;
                SetQueueStatus();
            }
        }

        void IFifoReadable.Close()
        {
            try
            {
                lock (this)
                {
                    _isClosedByReader = true;
                    SetQueueStatus();
                }
            }
            catch (Exception)
            {
                // close の最中の例外は無視する
            }
        }

        void IFifoWritable.Write(byte[] buffer, int offset, int count)
        {
            var totalCount = 0;
            while (totalCount < count)
            {
                var length = InternalWrite(buffer, offset + totalCount, count - totalCount);
                totalCount += length;
            }
        }

        void IFifoWritable.WaitForReadCount(long count, CancellationToken token)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            try
            {
                lock (this)
                {
                    if (_expectedReadCount != long.MaxValue)
                        throw new InvalidOperationException();
                    if (_isClosedByReader)
                        throw new IOException("The stream is already closed by reader.");
                    if (_isClosedByWriter)
                        throw new IOException("The stream is already closed by writer.");
                    _expectedReadCount = count;
                    SetQueueStatus();
                }
                while (true)
                {
                    _isReadyForReadCountEvent.Wait(token);
                    lock (this)
                    {
                        if (_isClosedByReader)
                            throw new IOException("The stream is already closed by reader.");
                        if (_isClosedByWriter)
                            throw new IOException("The stream is already closed by writer.");
                        if (_totalReadCount >= count)
                            return;
                    }
                }
            }
            finally
            {
                lock (this)
                {
                    if (_expectedReadCount != long.MaxValue)
                    {
                        _expectedReadCount = long.MaxValue;
                        SetQueueStatus();
                    }
                }
            }
        }

        void IFifoWritable.Close()
        {
            try
            {
                lock (this)
                {
                    _isClosedByWriter = true;
                    SetQueueStatus();
                }
            }
            catch (Exception)
            {
                // close の最中の例外は無視する
            }
        }
    }
}
