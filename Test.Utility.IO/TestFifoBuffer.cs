using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utility;
using Utility.IO;

namespace Test.Utility.IO
{
    static class TestFifoBuffer
    {
        private static readonly FileInfo _testFile = new(Path.Combine(Path.GetDirectoryName(typeof(TestFifoBuffer).Assembly.Location) ?? ".", "testData.txt"));

        public static void Test()
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
            .ForEach(item => TestFifo(_testFile, item.waitOnOutput, item.writerBufferSize, item.waitOnInput, item.readerBufferSize));
        }

        private static void TestFifo(FileInfo testFile, bool waitOnOutput, Int32 writerBufferSize, bool waitOnInput, Int32 readerBufferSize)
        {
            var fifo = new ByteIOQueue(8);
            var readerTask =
                Task.Run(() =>
                {
                    using var inputStream = testFile.OpenRead();
                    using var outputStream = fifo.GetWriter();
                    var buffer = new byte[writerBufferSize];
                    while (true)
                    {
                        var length = inputStream.Read(buffer, 0, buffer.Length);
                        if (length <= 0)
                            break;
                        outputStream.Write(buffer.AsSpan(0, length));
                        if (waitOnOutput)
                            Thread.Sleep(100);
                        Console.Write(".");
                    }
                    outputStream.Flush();
                });

            var verifierTask =
                Task.Run(() =>
                {
                    var testData = testFile.ReadAllBytes();
                    var index = 0;
                    var buffer = new byte[readerBufferSize];
                    using var inputStream = fifo.GetReader();
                    while (true)
                    {
                        var length = inputStream.Read(buffer, 0, buffer.Length);
                        if (length <= 0)
                            break;
                        if (index + length > testData.Length)
                            return false;
                        if (!testData.AsSpan(index, length).SequenceEqual(buffer.AsSpan(0, length)))
                            return false;
                        index += length;
                        if (waitOnInput)
                            Thread.Sleep(100);
                        Console.Write(".");
                    }
                    return index == testData.Length;
                });

            readerTask.Wait();
            var result = verifierTask.Result;
            if (!result)
                throw new Exception();
        }
    }
}
