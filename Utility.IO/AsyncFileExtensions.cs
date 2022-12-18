using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Utility.IO
{
    public static class AsyncFileExtensions
    {
        private class ReadLinesEnumerable
            : IAsyncEnumerable<string>
        {
            private class Enumerator
                : IAsyncEnumerator<string>
            {
                private readonly TextReader _reader;
                private readonly CancellationToken _cancellationToken;

                private bool _isDisposed;
                private string? _currentValue;
                private bool _endOfStream;

                public Enumerator(TextReader reader, CancellationToken cancellationToken)
                {
                    _reader = reader;
                    _cancellationToken = cancellationToken;
                    _isDisposed = false;
                    _currentValue = null;
                    _endOfStream = false;
                }

                public string Current
                {
                    get
                    {
                        if (_isDisposed)
                            throw new ObjectDisposedException(GetType().FullName);
                        if (_currentValue is null)
                            throw new InvalidOperationException();
                        if (_endOfStream)
                            throw new InvalidOperationException();
                        _cancellationToken.ThrowIfCancellationRequested();

                        return _currentValue;
                    }
                }

                public async ValueTask<bool> MoveNextAsync()
                {
                    if (_isDisposed)
                        throw new ObjectDisposedException(GetType().FullName);
                    _cancellationToken.ThrowIfCancellationRequested();

                    if (_endOfStream)
                        return false;
                    _currentValue = await _reader.ReadLineAsync().ConfigureAwait(false);
                    if (_currentValue is null)
                    {
                        _endOfStream = true;
                        return false;
                    }
                    return true;
                }

                public ValueTask DisposeAsync()
                {
                    if (!_isDisposed)
                    {
                        _reader.Dispose();
                        _isDisposed = true;
                    }
                    GC.SuppressFinalize(this);
                    return ValueTask.CompletedTask;
                }
            }

            private readonly TextReader _reader;

            public ReadLinesEnumerable(TextReader reader)
            {
                _reader = reader;
            }

            public IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
                new Enumerator(_reader, cancellationToken);
        }

        public static Task<byte[]> ReadAllBytesAsync(this FileInfo file, CancellationToken cancellationToken = default)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));

            return File.ReadAllBytesAsync(file.FullName, cancellationToken);
        }

        public static Task<string[]> ReadAllLinesAsync(this FileInfo file, CancellationToken cancellationToken = default)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));

            return File.ReadAllLinesAsync(file.FullName, cancellationToken);
        }

        public static IAsyncEnumerable<string> ReadLinesAsync(this FileInfo file)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));

            return new ReadLinesEnumerable(new StreamReader(file.FullName, Encoding.UTF8));
        }

        public static IAsyncEnumerable<string> ReadLinesAsync(this FileInfo file, Encoding encoding)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));
            if (encoding is null)
                throw new ArgumentNullException(nameof(encoding));

            return new ReadLinesEnumerable(new StreamReader(file.FullName, encoding));
        }

        public static async Task WriteAllBytesAsync(this FileInfo file, IEnumerable<byte> data, CancellationToken cancellationToken = default)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            var stream = file.OpenWrite();
            await using (stream.ConfigureAwait(false))
            {
                await stream.WriteByteSequenceAsync(data, cancellationToken).ConfigureAwait(false);
            }
        }

        public static async Task WriteAllBytesAsync(this FileInfo file, IAsyncEnumerable<byte> data, CancellationToken cancellationToken = default)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            var stream = file.OpenWrite();
            await using (stream.ConfigureAwait(false))
            {
                await stream.WriteByteSequenceAsync(data, cancellationToken).ConfigureAwait(false);
            }
        }

        public static async Task WriteAllBytesAsync(this FileInfo file, byte[] data, CancellationToken cancellationToken = default)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            await File.WriteAllBytesAsync(file.FullName, data, cancellationToken).ConfigureAwait(false);
        }

        public static async Task WriteAllBytesAsync(this FileInfo file, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));

            var stream = file.OpenWrite();
            await using (stream.ConfigureAwait(false))
            {
                await stream.WriteBytesAsync(data, cancellationToken).ConfigureAwait(false);
            }
        }

        public static async Task WriteAllTextAsync(this FileInfo file, string text, CancellationToken cancellationToken = default)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            await File.WriteAllTextAsync(file.FullName, text, cancellationToken).ConfigureAwait(false);
        }

        public static async Task WriteAllTextAsync(this FileInfo file, string text, Encoding encoding, CancellationToken cancellationToken = default)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));
            if (text is null)
                throw new ArgumentNullException(nameof(text));
            if (encoding is null)
                throw new ArgumentNullException(nameof(encoding));

            await File.WriteAllTextAsync(file.FullName, text, encoding, cancellationToken).ConfigureAwait(false);
        }

        public static async Task WriteAllLinesAsync(this FileInfo file, IEnumerable<string> lines, CancellationToken cancellationToken = default)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));
            if (lines is null)
                throw new ArgumentNullException(nameof(lines));

            await File.WriteAllLinesAsync(file.FullName, lines, cancellationToken).ConfigureAwait(false);
        }

        public static async Task WriteAllTextAsync(this FileInfo file, IEnumerable<string> lines, Encoding encoding, CancellationToken cancellationToken = default)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));
            if (lines is null)
                throw new ArgumentNullException(nameof(lines));
            if (encoding is null)
                throw new ArgumentNullException(nameof(encoding));

            await File.WriteAllLinesAsync(file.FullName, lines, encoding, cancellationToken).ConfigureAwait(false);
        }

        public static async Task WriteAllLinesAsync(this FileInfo file, IAsyncEnumerable<string> lines, CancellationToken cancellationToken = default)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));
            if (lines is null)
                throw new ArgumentNullException(nameof(lines));

            await InternalWriteAllLinesAsync(file, lines, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        }

        public static async Task WriteAllTextAsync(this FileInfo file, IAsyncEnumerable<string> lines, Encoding encoding, CancellationToken cancellationToken = default)
        {
            if (file is null)
                throw new ArgumentNullException(nameof(file));
            if (lines is null)
                throw new ArgumentNullException(nameof(lines));
            if (encoding is null)
                throw new ArgumentNullException(nameof(encoding));

            await InternalWriteAllLinesAsync(file, lines, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        }


        public static Task<(UInt32 Crc, UInt64 Length)> CalculateCrc24Async(this FileInfo sourceFile, CancellationToken cancellationToken = default)
        {
            if (sourceFile is null)
                throw new ArgumentNullException(nameof(sourceFile));

            return sourceFile.OpenRead().CalculateCrc24Async(cancellationToken);
        }

        public static Task<(UInt32 Crc, UInt64 Length)> CalculateCrc24Async(this FileInfo sourceFile, IProgress<UInt64>? progress, CancellationToken cancellationToken = default)
        {
            if (sourceFile is null)
                throw new ArgumentNullException(nameof(sourceFile));

            return sourceFile.OpenRead().CalculateCrc24Async(progress, cancellationToken);
        }

        public static Task<(UInt32 Crc, UInt64 Length)> CalculateCrc32Async(this FileInfo sourceFile, CancellationToken cancellationToken = default)
        {
            if (sourceFile is null)
                throw new ArgumentNullException(nameof(sourceFile));

            return sourceFile.OpenRead().CalculateCrc32Async(cancellationToken);
        }

        public static Task<(UInt32 Crc, UInt64 Length)> CalculateCrc32Async(this FileInfo sourceFile, IProgress<UInt64>? progress, CancellationToken cancellationToken = default)
        {
            if (sourceFile is null)
                throw new ArgumentNullException(nameof(sourceFile));

            return sourceFile.OpenRead().CalculateCrc32Async(progress, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task InternalWriteAllLinesAsync(FileInfo file, IAsyncEnumerable<string> lines, Encoding encoding, CancellationToken cancellationToken)
        {
            var writer = new StreamWriter(file.FullName, false, encoding);
            await using (writer.ConfigureAwait(false))
            {
                var enumerator = lines.GetAsyncEnumerator(cancellationToken);
                await using (enumerator.ConfigureAwait(false))
                {
                    while (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        writer.WriteLine(enumerator.Current);
                    }
                }
                await writer.FlushAsync().ConfigureAwait(false);
            }
        }
    }
}
