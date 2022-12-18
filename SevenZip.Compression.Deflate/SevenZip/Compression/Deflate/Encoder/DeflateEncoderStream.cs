using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Utility.IO;

namespace SevenZip.Compression.Deflate.Encoder
{
    public class DeflateEncoderStream
        : IBasicOutputByteStream
    {
        private readonly InternalDeflateEncoderStream _stream;

        public DeflateEncoderStream(IBasicOutputByteStream baseStream, DeflateEncoderProperties properties, IProgress<UInt64>? progress, bool leaveOpen)
        {
            _stream = new InternalDeflateEncoderStream(true, baseStream, properties.InternalProperties, progress, leaveOpen);
        }

        public Int32 Write(ReadOnlySpan<byte> buffer) => _stream.Write(buffer);
        public Task<Int32> WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => _stream.WriteAsync(buffer, cancellationToken);
        public void Flush() => _stream.Flush();
        public Task FlushAsync(CancellationToken cancellationToken = default) => _stream.FlushAsync(cancellationToken);

        [SuppressMessage("Usage", "CA1816:Dispose メソッドは、SuppressFinalize を呼び出す必要があります", Justification = "<保留中>")]
        public void Dispose() => _stream.Dispose();

        [SuppressMessage("Usage", "CA1816:Dispose メソッドは、SuppressFinalize を呼び出す必要があります", Justification = "<保留中>")]
        public ValueTask DisposeAsync() => _stream.DisposeAsync();
    }
}
