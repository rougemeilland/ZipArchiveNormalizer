using System;
using System.IO;
using Utility.IO;

namespace Test.Utility.IO
{
    static class TestBufferedInputStream
    {
        private static FileInfo _testFile = new FileInfo(Path.Combine(Path.GetDirectoryName(typeof(TestBufferedInputStream).Assembly.Location), "testData.txt"));

        public static void Test()
        {
            const int streamBufferSize = 1024;
            const int bufferSize = 15;

            var buffer = new byte[bufferSize];
            using (var ms = new MemoryStream())
            {
                using (var inputStream = _testFile.OpenRead().AsInputByteStream().WithCache(streamBufferSize))
                {
                    while (true)
                    {
                        var length = inputStream.Read(buffer, 0, buffer.Length);
                        if (length <= 0)
                            break;
                        ms.Write(buffer, 0, length);
                        Console.Write(".");
                    }
                    ms.Flush();
                }
                ms.Position = 0;
                if (ms.StreamBytesEqual(_testFile.OpenRead()) == false)
                    throw new Exception();
            }
        }
    }
}
