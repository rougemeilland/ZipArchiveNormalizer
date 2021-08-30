using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Utility
{
    public static class StreamExtensions
    {
        private class ByteSequenceEnumerable
            : IEnumerable<byte>
        {
            private class Enumerator
                : IEnumerator<byte>
            {
                private bool _isDisposed;
                private Stream _inputStream;
                private long? _offset;
                private long? _count;
                private bool _leaveOpen;
                private byte[] _buffer;
                private int _bufferCount;
                private int _bufferIndex;
                private long _index;

                public Enumerator(Stream inputStream, long? offset, long? count, bool leaveOpen)
                {
                    _isDisposed = false;
                    _inputStream = inputStream;
                    _offset = offset;
                    _count = count;
                    _leaveOpen = leaveOpen;
                    _buffer = new byte[64 * 1024];
                    _bufferCount = 0;
                    _bufferIndex = 0;
                    _index = 0;
                    if (offset.HasValue)
                        inputStream.Seek(offset.Value, SeekOrigin.Begin);
                }

                public byte Current
                {
                    get
                    {
                        // 既にオブジェクトが破棄されていれば例外
                        if (_isDisposed)
                            throw new ObjectDisposedException(GetType().FullName.ToString());
                        // 既にEOSに達していれば例外
                        if (_count.HasValue && _index > _count.Value)
                            throw new InvalidOperationException();
                        if (_bufferIndex >= _bufferCount)
                            throw new InvalidOperationException();
                        // 現在指しているデータを返す
                        return _buffer[_bufferIndex];
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    // 既にオブジェクトが破棄されていれば例外
                    if (_isDisposed)
                        throw new ObjectDisposedException(GetType().FullName.ToString());
                    // index を一つ進める
                    ++_bufferIndex;
                    ++_index;
                    if (_count.HasValue && _index > _count)
                    {
                        // 指定された回数だけ繰り返し終わった場合
                        return false;
                    }
                    if (_bufferIndex >= _bufferCount)
                    {
                        // _buffer のデータの最後に到達した場合
                        // _sourceStream から新たなデータを読み込もうと試みる
                        _bufferCount = _inputStream.Read(_buffer, 0, _buffer.Length);
                        if (_bufferCount <= 0)
                        {
                            // _sourceStream の終端に達してしまった場合
                            return false;
                        }
                        _bufferIndex = 0;
                    }
#if DEBUG
                    if (_bufferIndex >= _bufferCount)
                        throw new Exception();
#endif
                    return true;
                }

                public void Reset()
                {
                    if (!_inputStream.CanSeek)
                        throw new NotSupportedException();
                    _bufferCount = 0;
                    _bufferIndex = 0;
                    _index = 0;
                    if (_offset.HasValue)
                        _inputStream.Seek(_offset.Value, SeekOrigin.Begin);
                }

                protected virtual void Dispose(bool disposing)
                {
                    if (!_isDisposed)
                    {
                        if (disposing)
                        {
                        }

                        if (_inputStream != null)
                        {
                            if (_leaveOpen == false)
                                _inputStream.Dispose();
                            _inputStream = null;
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

            private Stream _inputStream;
            private long? _offset;
            private long? _count;
            private bool _leaveOpen;

            public ByteSequenceEnumerable(Stream inputStream, long? offset, long? count, bool leaveOpen)
            {
                _inputStream = inputStream;
                _offset = offset;
                _count = count;
                _leaveOpen = leaveOpen;
            }

            public IEnumerator<byte> GetEnumerator()
            {
                return new Enumerator(_inputStream, _offset, _count, _leaveOpen);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class ReverseByteSequence
            : IEnumerable<byte>
        {
            private class Enumerator
                : IEnumerator<byte>
            {
                private const int _bufferSize = 64 * 1024;
                private bool _isDisposed;
                private Stream _inputStream;
                private long _offset;
                private long _count;
                private bool _leaveOpen;
                private byte[] _buffer;
                private int _bufferCount;
                private int _bufferIndex;
                private long _FileIndex;

                public Enumerator(Stream inputStream, long offset, long count, bool leaveOpen)
                {
                    _inputStream = inputStream;
                    _offset = offset;
                    _count = count;
                    _leaveOpen = leaveOpen;
                    _buffer = new byte[_bufferSize];
                    _bufferCount = 0;
                    _bufferIndex = 0;
                    _FileIndex = _offset + _count;
                }

                public byte Current => _bufferIndex.IsBetween(0, _bufferCount - 1) ? _buffer[_bufferIndex] : throw new InvalidOperationException();

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    if (_bufferIndex <= 0)
                    {
                        var newFileIndex = _FileIndex - _bufferSize;
                        if (newFileIndex < _offset)
                            newFileIndex = _offset;
                        _bufferCount = (int)(_FileIndex - newFileIndex);
                        if (_bufferCount <= 0)
                            return false;
                        _FileIndex = newFileIndex;
                        _inputStream.Seek(_FileIndex, SeekOrigin.Begin);
                        _inputStream.ReadBytes(_buffer, 0, _bufferCount);
                        _bufferIndex = _bufferCount;
                    }
                    --_bufferIndex;
                    return true;
                }

                public void Reset()
                {
                    _bufferCount = -1;
                    _bufferIndex = -1;
                    _FileIndex = -1;
                }

                protected virtual void Dispose(bool disposing)
                {
                    if (!_isDisposed)
                    {
                        if (disposing)
                        {
                        }

                        if (_inputStream != null)
                        {
                            if (_leaveOpen == false)
                                _inputStream.Dispose();
                            _inputStream = null;
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

            private Stream _inputStream;
            private long _offset;
            private long _count;
            private bool _leaveOpen;

            public ReverseByteSequence(Stream inputStream, long offset, long count, bool leaveOpen)
            {
                _inputStream = inputStream;
                _offset = offset;
                _count = count;
                _leaveOpen = leaveOpen;
            }

            public IEnumerator<byte> GetEnumerator()
            {
                return new Enumerator(_inputStream, _offset, _count, _leaveOpen);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public static IEnumerable<byte> GetByteSequence(this Stream inputStream, bool leaveOpen = false)
        {
            return new ByteSequenceEnumerable(inputStream, null, null, leaveOpen);
        }

        public static IEnumerable<byte> GetByteSequence(this Stream inputStream, long offset, bool leaveOpen = false)
        {
            if (inputStream.CanSeek == false)
                throw new ArgumentException();
            if (offset < 0)
                throw new ArgumentException();
            if (offset > inputStream.Length)
                throw new ArgumentException();
            return new ByteSequenceEnumerable(inputStream, offset, inputStream.Length - offset, leaveOpen);
        }

        public static IEnumerable<byte> GetByteSequence(this Stream inputStream, long offset, long count, bool leaveOpen = false)
        {
            if (inputStream.CanSeek == false)
                throw new ArgumentException();
            if (offset < 0)
                throw new ArgumentException();
            if (count < 0)
                throw new ArgumentException();
            if (offset + count > inputStream.Length)
                throw new ArgumentException();
            return new ByteSequenceEnumerable(inputStream, offset, count, leaveOpen);
        }

        public static IEnumerable<byte> GetReverseByteSequence(this Stream inputStream, bool leaveOpen = false)
        {
            if (inputStream.CanSeek == false)
                throw new ArgumentException();
            return new ReverseByteSequence(inputStream, 0, inputStream.Length, leaveOpen);
        }

        public static IEnumerable<byte> GetReverseByteSequence(this Stream inputStream, long offset, bool leaveOpen = false)
        {
            if (inputStream.CanSeek == false)
                throw new ArgumentException();
            if (offset < 0)
                throw new ArgumentException();
            if (offset > inputStream.Length)
                throw new ArgumentException();
            return new ReverseByteSequence(inputStream, offset, inputStream.Length - offset, leaveOpen);
        }

        public static IEnumerable<byte> GetReverseByteSequence(this Stream inputStream, long offset, long count, bool leaveOpen = false)
        {
            if (inputStream.CanSeek == false)
                throw new ArgumentException();
            if (offset < 0)
                throw new ArgumentException();
            if (count < 0)
                throw new ArgumentException();
            if (offset + count > inputStream.Length)
                throw new ArgumentException();
            return new ReverseByteSequence(inputStream, offset, count, leaveOpen);
        }

        public static bool StreamBytesEqual(this Stream stream1, Stream stream2, bool leaveOpen = false)
        {
            const int bufferSize = 64 * 1024;
#if DEBUG
            if (bufferSize % sizeof(UInt64) != 0)
                throw new Exception();
#endif
            try
            {
                if (stream1 == null)
                    throw new ArgumentNullException();
                if (stream2 == null)
                    throw new ArgumentNullException();
                var buffer1 = new byte[bufferSize];
                var buffer2 = new byte[bufferSize];
                while (true)
                {
                    // まず両方のストリームから bufferSize バイトだけ読み込みを試みる
                    var bufferCount1 = stream1.ReadBytes(buffer1, 0, buffer1.Length);
                    var bufferCount2 = stream2.ReadBytes(buffer2, 0, buffer2.Length);
                    if (bufferCount1 != bufferCount2)
                    {
                        // 実際に読み込めたサイズが異なっている場合はどちらかだけがEOFに達したということなので、ストリームの内容が異なると判断しfalseを返す。
                        return false;
                    }

                    // この時点で bufferCount1 == bufferCount2 (どちらのストリームも読み込めたサイズは同じ)

                    if (buffer1.ByteArrayEqual(0, buffer2, 0, bufferCount1) == false)
                    {
                        // バッファの内容が一致しなかった場合は false を返す。
                        return false;
                    }

                    if (bufferCount1 != buffer1.Length)
                    {
                        // どちらのストリームも同時にEOFに達したがそれまでに読み込めたデータはすべて一致していた場合
                        // 全てのデータが一致したと判断して true を返す。
                        return true;
                    }
                }
            }
            finally
            {
                if (leaveOpen == false)
                {
                    stream1.Dispose();
                    stream2.Dispose();
                }
            }
        }

        public static IReadOnlyArray<byte> ReadBytes(this Stream source, int count)
        {
            if (count < 0)
                throw new ArgumentException("count");
            var buffer = new byte[count];
            var index = 0;
            while (index < buffer.Length)
            {
                var length = source.Read(buffer, index, buffer.Length - index);
                if (length <= 0)
                    break;
                index += length;
            }
            if (index < buffer.Length)
                Array.Resize(ref buffer, index);
            return buffer.AsReadOnly();
        }

        public static int ReadBytes(this Stream source, byte[] buffer, int offset, int count)
        {
            if (count < 0)
                throw new ArgumentException("count");
            var index = offset;
            while (index < offset + count)
            {
                var length = source.Read(buffer, index, offset + count - index);
                if (length <= 0)
                    break;
                index += length;
            }
            return index - offset;
        }

        public static IReadOnlyArray<byte> ReadBytes(this Stream source, long count)
        {
            if (count < 0)
                throw new ArgumentException("count");
            var byteArrayChain = new byte[0].AsEnumerable();
            while (count > 0)
            {
                var length = (int)Math.Min(count, int.MaxValue);
                var data = source.ReadBytes(length);
                byteArrayChain = byteArrayChain.Concat(data);
                count -= length;
            }
            return byteArrayChain.ToArray().AsReadOnly();
        }

        public static IReadOnlyArray<byte> ReadAllBytes(this Stream source)
        {
            using (var outputStream = new MemoryStream())
            {
                source.CopyTo(outputStream);
                return outputStream.ToArray().AsReadOnly();
            }
        }

#if DEBUG
        public static void SelfTest()
        {
            var testData1 = Encoding.UTF8.GetBytes("");
            using (var inputStream = new MemoryStream(testData1))
            {
                if (!inputStream.GetByteSequence(true).ToArray().SequenceEqual(testData1))
                    throw new Exception();
            }
            var testData2 = Encoding.UTF8.GetBytes("Hello");
            using (var inputStream = new MemoryStream(testData2))
            {
                if (!inputStream.GetByteSequence(true).ToArray().SequenceEqual(testData2))
                    throw new Exception();
            }
            var testData3 = Encoding.UTF8.GetBytes("1995年夏、人々は融けかかったアスファルトに己が足跡を刻印しつつ歩いていた。酷く暑い.");
            using (var inputStream = new MemoryStream(testData3))
            {
                if (!inputStream.GetByteSequence(true).ToArray().SequenceEqual(testData3))
                    throw new Exception();
            }
            var testData4 = Encoding.UTF8.GetBytes("1995年夏、人々は溶けかかったアスファルトに己が足跡を刻印しつつ歩いていた。酷く暑い!");
            var testData5 = Encoding.UTF8.GetBytes("1995年夏、人々は溶けかかったアスファルトに己が足跡を刻印しつつ歩いていた。酷く暑い!!");
            var testData6 = Encoding.UTF8.GetBytes("2995年夏、人々は融けかかったアスファルトに己が足跡を刻印しつつ歩いていた。酷く暑い.");

            var testDataPair = new[]
            {
                new { data1 = testData1, data2 = testData1, expected = true},
                new { data1 = testData1, data2 = testData3, expected = false},
                new { data1 = testData1, data2 = testData4, expected = false},
                new { data1 = testData1, data2 = testData5, expected = false},
                new { data1 = testData1, data2 = testData6, expected = false},
                new { data1 = testData3, data2 = testData1, expected = false},
                new { data1 = testData3, data2 = testData3, expected = true},
                new { data1 = testData3, data2 = testData4, expected = false},
                new { data1 = testData3, data2 = testData5, expected = false},
                new { data1 = testData3, data2 = testData6, expected = false},
                new { data1 = testData4, data2 = testData1, expected = false},
                new { data1 = testData4, data2 = testData3, expected = false},
                new { data1 = testData4, data2 = testData4, expected = true},
                new { data1 = testData4, data2 = testData5, expected = false},
                new { data1 = testData4, data2 = testData6, expected = false},
                new { data1 = testData5, data2 = testData1, expected = false},
                new { data1 = testData5, data2 = testData3, expected = false},
                new { data1 = testData5, data2 = testData4, expected = false},
                new { data1 = testData5, data2 = testData5, expected = true},
                new { data1 = testData5, data2 = testData6, expected = false},
                new { data1 = testData6, data2 = testData1, expected = false},
                new { data1 = testData6, data2 = testData3, expected = false},
                new { data1 = testData6, data2 = testData4, expected = false},
                new { data1 = testData6, data2 = testData5, expected = false},
                new { data1 = testData6, data2 = testData6, expected = true},
            };
            foreach (var item in testDataPair)
            {
                using (var inputStream1 = new MemoryStream(item.data1))
                using (var inputStream2 = new MemoryStream(item.data2))
                {
                    if (inputStream1.StreamBytesEqual(inputStream2, true) != item.expected)
                        throw new Exception();
                }
            }
        }
#endif
    }
}