using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utility;
using Utility.IO;

namespace Test.Utility.IO
{
    class Program
    {
        static void Main(string[] args)
        {
            var testFile = new FileInfo(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "testData.txt"));

            TestInputBufferedStream(testFile);
            TestOutputBufferedStream(testFile, false);
            TestOutputBufferedStream(testFile, true);
            if (true)
            {
                new[]
                {
                new { waitOnOutput = true, waitOnInput = false },
                new { waitOnOutput = false, waitOnInput = true },
            }
                .SelectMany(
                    item1 => new[]
                    {
                    new { writerBufferSize = 3, readerBufferSize = 5 },
                    new { writerBufferSize = 5, readerBufferSize = 3 },
                    },
                    (item1, item2) => new { item1.waitOnOutput, item2.writerBufferSize, item1.waitOnInput, item2.readerBufferSize })
                .SelectMany(item => new[] { false, true }, (item, sync) => new { item.waitOnOutput, item.writerBufferSize, item.waitOnInput, item.readerBufferSize, sync })
                .ForEach(item => TestFifo(testFile, item.waitOnOutput, item.writerBufferSize, item.waitOnInput, item.readerBufferSize, item.sync));
            }
            Console.WriteLine();
            Console.WriteLine("OK");
            Console.Beep();
            Console.ReadLine();
        }

        private static void TestFifo(FileInfo testFile, bool waitOnOutput, int writerBufferSize, bool waitOnInput, int readerBufferSize, bool sync)
        {
            var fifo = new FifoBuffer(8);
            var readerTask =
                Task.Run(() =>
                {
                    using (var inputStream = testFile.OpenRead())
                    using (var outputStream = fifo.GetOutputStream(sync))
                    {
                        var buffer = new byte[writerBufferSize];
                        while (true)
                        {
                            var length = inputStream.Read(buffer, 0, buffer.Length);
                            if (length <= 0)
                                break;
                            outputStream.Write(buffer, 0, length);
                            if (waitOnOutput)
                                Thread.Sleep(100);
                            Console.Write(".");
                        }
                        outputStream.Flush();
                    }
                });

            var verifierTask =
                Task.Run(() =>
                {
                    var testData = testFile.ReadAllBytes();
                    var index = 0;
                    var buffer = new byte[readerBufferSize];
                    using (var inputStream = fifo.GetInputStream())
                    {
                        while (true)
                        {
                            var length = inputStream.Read(buffer, 0, buffer.Length);
                            if (length <= 0)
                                break;
                            if (index + length > testData.Length)
                                return false;
                            if (testData.ByteArrayEqual(index, buffer, 0, length) == false)
                                return false;
                            index += length;
                            if (waitOnInput)
                                Thread.Sleep(100);
                            Console.Write(".");
                        }
                        return index == testData.Length;
                    }
                });

            readerTask.Wait();
            var result = verifierTask.Result;
            if (result == false)
                throw new Exception();
        }

        private static void TestInputBufferedStream(FileInfo testFile)
        {
            const int streamBufferSize = 1024;
            const int bufferSize = 15;

            var buffer = new byte[bufferSize];
            using (var ms = new MemoryStream())
            {
                using (var inputStream = new BufferedInputStream(testFile.OpenRead(), streamBufferSize, false))
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
                if (ms.StreamBytesEqual(testFile.OpenRead()) == false)
                    throw new Exception();
            }
        }

        private static void TestOutputBufferedStream(FileInfo testFile, bool doFlush)
        {
            const int streamBufferSize = 1024;
            const int bufferSize = 15;

            var buffer = new byte[bufferSize];
            using (var ms = new MemoryStream())
            {
                using (var inputStream = testFile.OpenRead())
                using (var outputStream = new BufferedOutputStream(ms, streamBufferSize, true))
                {
                    while (true)
                    {
                        var length = inputStream.Read(buffer, 0, buffer.Length);
                        if (length <= 0)
                            break;
                        outputStream.Write(buffer, 0, length);
                        Console.Write(".");
                    }
                    if (doFlush)
                        outputStream.Flush();
                }
                ms.Position = 0;
                if (ms.StreamBytesEqual(testFile.OpenRead()) == false)
                    throw new Exception();
            }
        }
    }
}
