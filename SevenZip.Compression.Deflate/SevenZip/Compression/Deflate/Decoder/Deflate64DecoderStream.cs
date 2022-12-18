using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Utility.IO;

namespace SevenZip.Compression.Deflate.Decoder
{
    public class Deflate64DecoderStream
        : IBasicInputByteStream
    {
        private readonly InternalDeflateDecoderStream _baseStream;

        public Deflate64DecoderStream(IBasicInputByteStream baseStream, UInt64? size, IProgress<UInt64>? progress, bool leaveOpen)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                _baseStream = new InternalDeflateDecoderStream(true, baseStream, size, progress, leaveOpen);
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public Int32 Read(Span<byte> buffer) => _baseStream.Read(buffer);
        public Task<Int32> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _baseStream.ReadAsync(buffer, cancellationToken);

        [SuppressMessage("Usage", "CA1816:Dispose メソッドは、SuppressFinalize を呼び出す必要があります", Justification = "<保留中>")]
        public void Dispose() => _baseStream.Dispose();

        [SuppressMessage("Usage", "CA1816:Dispose メソッドは、SuppressFinalize を呼び出す必要があります", Justification = "<保留中>")]
        public ValueTask DisposeAsync() => _baseStream.DisposeAsync();
    }
}
