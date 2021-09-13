using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Utility.IO;
using Utility;
using ZipUtility;
using ZipUtility.Compression;

namespace Experiment
{
    class Program
    {
        static void Main(string[] args)
        {
            var compressionMethods = new[]
            {
                ZipEntryCompressionMethod.BZIP2,
                ZipEntryCompressionMethod.DeflateWithFast,
                ZipEntryCompressionMethod.DeflateWithMaximum,
                ZipEntryCompressionMethod.DeflateWithNormal,
                ZipEntryCompressionMethod.DeflateWithSuperFast,
                ZipEntryCompressionMethod.LZMAWithEOS,
                ZipEntryCompressionMethod.LZMAWithoutEOS,
                ZipEntryCompressionMethod.Stored,
            };
            var testFile = new FileInfo(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.FullName), "TestData.txt"));
            var size = testFile.Length;
            foreach (var compressionMethod in compressionMethods)
            {
                using (var ms = new MemoryStream())
                {
                    using (var inputStream = testFile.OpenRead())
                    {
                        using (var outputStream = compressionMethod.GetOutputStream(ms, 0, size, true))
                        {
                            inputStream.CopyTo(outputStream);
                        }
                    }
                    var packedSize = ms.Position;
                    ms.Seek(0, SeekOrigin.Begin);
                    using (var inputStream1 = testFile.OpenRead())
                    {
                        using (var inputStream2 = compressionMethod.GetInputStream(ms, 0, packedSize, size, true))
                        {
                            if (!inputStream1.StreamBytesEqual(inputStream2))
                                throw new Exception();
                        }
                    }
                }
            }
            Console.ReadLine();
        }
    }
}