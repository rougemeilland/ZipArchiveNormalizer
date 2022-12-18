using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.IO;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utility.Threading;

namespace Experiment
{
    public class Program
    {
        static void Main()
        {

            TestSharpCompressLzma();

            Console.WriteLine("Completed");
            Console.Beep();
            Console.ReadLine();
        }

        private static void TestSharpCompressLzma()
        {
            Directory.CreateDirectory(SCRATCH_FILES_PATH);
            ArchiveStreamRead("Zip.lzma.zip");
            ArchiveFileRead("Zip.lzma.zip");
            Read("Zip.lzma.zip", CompressionType.LZMA);
        }

        private static class Assert
        {
            public static void False(bool result)
            {
                if (result != false)
                    throw new Exception();
            }

            public static void False(bool result, string message)
            {
                if (result != false)
                    throw new Exception(message);
            }

            public static void True(bool result)
            {
                if (result != true)
                    throw new Exception();
            }

            public static void True(bool result, string message)
            {
                if (result != true)
                    throw new Exception(message);
            }

            public static void Equal(object x, object y)
            {
                if (!x.Equals(y))
                    throw new Exception();
            }

            public static void Equal(object x, object y, string message)
            {
                if (!x.Equals(y))
                    throw new Exception(message);
            }
        }


        private class ForwardOnlyStream : Stream
        {
            private readonly Stream stream;

            public bool IsDisposed { get; private set; }

            public ForwardOnlyStream(Stream stream)
            {
                this.stream = stream;
            }

            protected override void Dispose(bool disposing)
            {
                if (!IsDisposed)
                {
                    if (disposing)
                    {
                        stream.Dispose();
                        IsDisposed = true;
                        base.Dispose(disposing);
                    }
                }
            }

            public override bool CanRead => true;

            public override bool CanSeek => false;
            public override bool CanWrite => false;

            public override void Flush()
            {
                throw new NotSupportedException();
            }

            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return stream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }

        public class TestStream : Stream
        {
            private readonly Stream stream;

            public TestStream(Stream stream) : this(stream, stream.CanRead, stream.CanWrite, stream.CanSeek)
            {
            }

            public bool IsDisposed { get; private set; }

            public TestStream(Stream stream, bool read, bool write, bool seek)
            {
                this.stream = stream;
                CanRead = read;
                CanWrite = write;
                CanSeek = seek;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                stream.Dispose();
                IsDisposed = true;
            }

            public override bool CanRead { get; }

            public override bool CanSeek { get; }

            public override bool CanWrite { get; }

            public override void Flush()
            {
                stream.Flush();
            }

            public override long Length => stream.Length;

            public override long Position
            {
                get => stream.Position;
                set => stream.Position = value;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return stream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return stream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                stream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                stream.Write(buffer, offset, count);
            }
        }

        private const string TEST_ARCHIVES_PATH = @"D:\テストデータ\sharpcompress-master\Archives";
        private const string SCRATCH_FILES_PATH = @"Z:\Downloads\Lunor\work";
        private const string ORIGINAL_FILES_PATH = @"Z:\Downloads\Lunor\sharpcompress-0.30.1\sharpcompress-0.30.1\tests\TestArchives\Original";

        private const bool UseExtensionInsteadOfNameToVerify = true;


        private static void ArchiveStreamRead(string testArchive)
        {
            testArchive = Path.Combine(TEST_ARCHIVES_PATH, testArchive);
            ArchiveStreamRead(new string[] { testArchive });
        }

        private static void ArchiveStreamRead(IEnumerable<string> testArchives)
        {
            foreach (var path in testArchives)
            {
                using (var stream = new NonDisposingStream(File.OpenRead(path), true))
                using (var archive = ArchiveFactory.Open(stream, null))
                {
                    try
                    {
                        foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                        {
                            entry.WriteToDirectory(SCRATCH_FILES_PATH,
                                                   new ExtractionOptions()
                                                   {
                                                       ExtractFullPath = true,
                                                       Overwrite = true
                                                   });
                        }
                    }
                    catch (IndexOutOfRangeException)
                    {
                        //SevenZipArchive_BZip2_Split test needs this
                        stream.ThrowOnDispose = false;
                        throw;
                    }
                    stream.ThrowOnDispose = false;
                }
                VerifyFiles();
            }
        }

        private static void ArchiveFileRead(string testArchive)
        {
            testArchive = Path.Combine(TEST_ARCHIVES_PATH, testArchive);
            using (var archive = ArchiveFactory.Open(testArchive, null))
            {
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    entry.WriteToDirectory(SCRATCH_FILES_PATH,
                        new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                }
            }
            VerifyFiles();
        }

        private static         void Read(string testArchive, CompressionType expectedCompression)
        {
            testArchive = Path.Combine(TEST_ARCHIVES_PATH, testArchive);

            var options = new ReaderOptions
            {
                LeaveStreamOpen = true
            };
            ReadImpl(testArchive, expectedCompression, options);

            options.LeaveStreamOpen = false;
            ReadImpl(testArchive, expectedCompression, options);
            VerifyFiles();
        }

        private static void ReadImpl(string testArchive, CompressionType expectedCompression, ReaderOptions options)
        {
            using var file = File.OpenRead(testArchive);
            using var protectedStream = new NonDisposingStream(new ForwardOnlyStream(file), throwOnDispose: true);
            using var testStream = new TestStream(protectedStream);
            using (var reader = ReaderFactory.Open(testStream, options))
            {
                UseReader(reader, expectedCompression);
                protectedStream.ThrowOnDispose = false;
                Assert.False(testStream.IsDisposed, $"{nameof(testStream)} prematurely closed");
            }

            // Boolean XOR -- If the stream should be left open (true), then the stream should not be diposed (false)
            // and if the stream should be closed (false), then the stream should be disposed (true)
            var message = $"{nameof(options.LeaveStreamOpen)} is set to '{options.LeaveStreamOpen}', so {nameof(testStream.IsDisposed)} should be set to '{!testStream.IsDisposed}', but is set to {testStream.IsDisposed}";
            Assert.True(options.LeaveStreamOpen != testStream.IsDisposed, message);
        }

        private static void UseReader(IReader reader, CompressionType expectedCompression)
        {
            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory)
                {
                    Assert.Equal(expectedCompression, reader.Entry.CompressionType);
                    reader.WriteEntryToDirectory(SCRATCH_FILES_PATH, new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }
        }

        private static void VerifyFiles()
        {
            if (UseExtensionInsteadOfNameToVerify)
            {
                VerifyFilesByExtension();
            }
            else
            {
                //VerifyFilesByName();
            }


        }

        private static void VerifyFilesByExtension()
        {
            var extracted =
                Directory.EnumerateFiles(SCRATCH_FILES_PATH, "*.*", SearchOption.AllDirectories)
                .ToLookup(path => Path.GetExtension(path));
            var original =
                Directory.EnumerateFiles(ORIGINAL_FILES_PATH, "*.*", SearchOption.AllDirectories)
                .ToLookup(path => Path.GetExtension(path));

            Assert.Equal(extracted.Count, original.Count);

            foreach (var orig in original)
            {
                Assert.True(extracted.Contains(orig.Key));

                CompareFilesByPath(orig.Single(), extracted[orig.Key].Single());
            }
        }

        private static void  CompareFilesByPath(string file1, string file2)
        {
            //TODO: fix line ending issues with the text file
            if (file1.EndsWith("txt"))
            {
                return;
            }

            using var file1Stream = File.OpenRead(file1);
            using var file2Stream = File.OpenRead(file2);
            Assert.Equal(file1Stream.Length, file2Stream.Length);
            int byte1 = 0;
            int byte2 = 0;
            for (int counter = 0; byte1 != -1; counter++)
            {
                byte1 = file1Stream.ReadByte();
                byte2 = file2Stream.ReadByte();
                if (byte1 != byte2)
                {
                    //string.Format("Byte {0} differ between {1} and {2}", counter, file1, file2)
                    Assert.Equal(byte1, byte2);
                }
            }
        }



    }
}
