using System;
using System.IO;
using System.Linq;
using Utility;
using Utility.IO;
using ZipUtility;

namespace Test.ZipUtility.Compression
{
    class Program
    {
        static void Main(string[] args)
        {
            var lockObject = new object();

            var compressionMethods = new[]
            {
                ZipEntryCompressionMethod.Stored,
                ZipEntryCompressionMethod.DeflateWithFast,
                ZipEntryCompressionMethod.DeflateWithMaximum,
                ZipEntryCompressionMethod.DeflateWithNormal,
                ZipEntryCompressionMethod.DeflateWithSuperFast,
                ZipEntryCompressionMethod.BZIP2,
                //ZipEntryCompressionMethod.LZMAWithEOS,
                //ZipEntryCompressionMethod.LZMAWithoutEOS,
            };
            var testFileNames = new[]
            {
                "TEST_0バイトのファイル.txt",
                "TEST_非常に短いファイル0.txt",
                "TEST_普通のファイル1.txt",
                "TEST_1MBぐらいのファイル1.txt",
                "TEST_4GBよりやや短いファイル1.txt",
                "TEST_4GBを超えるが圧縮すると4GB未満のファイル1.txt",
                "TEST_圧縮しても4GBを超えるファイル1.txt",
                "TEST_程々に圧縮可能なファイル.txt",
            };

            var baseDirectoryPath = @"D:\テストデータ\source";
            var testItems =
                testFileNames
                    .Select(fileNmae => new FileInfo(Path.Combine(baseDirectoryPath, fileNmae)))
                    .SelectMany(file => compressionMethods, (file, method) => new { sourceFile = file, compressionMethod = method })
                    .Select((item, index) => new { index, item.sourceFile, item.compressionMethod })
                    .ToReadOnlyCollection();
            var totalCount = (UInt64)testItems.Sum(item => item.sourceFile.Length) * 2;

            var previousProgressText = "";
            var progress =
                new Progress<UInt64>(size =>
                {
                    lock (lockObject)
                    {
                        var progressText = string.Format("{0:F1}%\r", size * 100.0 / totalCount);
                        if (!string.Equals(progressText, previousProgressText, StringComparison.Ordinal))
                        {
                            Console.Write(progressText);
                            previousProgressText = progressText;
                        };
                    }
                });

            testItems
                .OrderBy(item =>
                    item.compressionMethod.CompressionMethodId == ZipEntryCompressionMethodId.LZMA
                    ? 0
                    : item.compressionMethod.CompressionMethodId == ZipEntryCompressionMethodId.BZIP2
                    ? 1
                    : 2)
                .ThenByDescending(item => item.sourceFile.Length)
                .ThenBy(item => item.index)
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(item =>
                {
                    if (item.sourceFile.Length <= 0 && item.compressionMethod.CompressionMethodId == ZipEntryCompressionMethodId.BZIP2)
                    {
                    }
                    else
                    {
                        var tempFile = new FileInfo(Path.GetTempFileName());
                        try
                        {
                            using (var inputStream = item.sourceFile.OpenRead().AsInputByteStream())
                            {
                                using (var tempFileStream = tempFile.OpenWrite().AsOutputByteStream())
                                using (var outputStream = item.compressionMethod.GetEncodingStream(tempFileStream, (UInt64)item.sourceFile.Length, progress))
                                {
                                    inputStream.CopyTo(outputStream);
                                }
                                using (var inputStream1 = item.sourceFile.OpenRead().AsInputByteStream())
                                using (var tempFileStream = tempFile.OpenRead().AsInputByteStream())
                                using (var inputStream2 = item.compressionMethod.GetDecodingStream(tempFileStream, (UInt64)item.sourceFile.Length, (UInt64)tempFile.Length, progress))
                                {
                                    var result = inputStream1.StreamBytesEqual(inputStream2, true);
                                    lock (lockObject)
                                    {
                                        Console.WriteLine(string.Format("{0}: file=\"{1}\", method={2}", result ? "OK" : "NG", item.sourceFile, item.compressionMethod.CompressionMethodId));
                                    }
                                }
                            }
                        }
                        finally
                        {
                            tempFile.Delete();
                        }
                    }
                });

            Console.WriteLine("          ");
            Console.WriteLine("Completed");
            Console.Beep();
            Console.ReadLine();
        }
    }
}
