using System;
using System.IO;
using Utility;
using Utility.IO;

namespace Test.Utility.IO
{
    static class TestBufferedOutputStream
    {
        private static readonly FileInfo _testFile = new(Path.Combine(Path.GetDirectoryName(typeof(TestBufferedOutputStream).Assembly.Location) ?? ".", "testData.txt"));

        public static void Test()
        {
            Test(false);
            Test(true);
        }

        private static void Test(bool doFlush)
        {
            const Int32 streamBufferSize = 1024;
            const Int32 bufferSize = 15;

            var buffer = new byte[bufferSize];
            using var ms = new MemoryStream();
            using (var inputStream = _testFile.OpenRead())
            using (var outputStream = ms.AsOutputByteStream().WithCache(streamBufferSize, true))
            {
                while (true)
                {
                    var length = inputStream.Read(buffer, 0, buffer.Length);
                    if (length <= 0)
                        break;
                    outputStream.WriteBytes(buffer);
                    Console.Write(".");
                }
                if (doFlush)
                    outputStream.Flush();
            }
            ms.Position = 0;
            if (!ms.StreamBytesEqual(_testFile.OpenRead()))
                throw new Exception();
        }
    }
}
