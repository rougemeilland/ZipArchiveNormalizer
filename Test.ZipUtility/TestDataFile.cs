using System;
using System.IO;
using System.Linq;
using System.Text;
using Utility;
using Utility.IO;

namespace Test.ZipUtility
{
    static class TestDataFile
    {
        public static void Create(DirectoryInfo baseDirectory)
        {
            var zeroFile = baseDirectory.GetFile("TEST_0バイトのファイル.txt");
            zeroFile.Delete();
            zeroFile.Create().Dispose();
            for (int count = 0; count < 3000; count++)
            {
                CreateRandomTextFile(baseDirectory.GetFile(string.Format("TEST_非常に短いファイル{0}.txt", count)), 5);
            }
            CreateRandomTextFile(baseDirectory.GetFile("TEST_普通のファイル.txt"), 10000);
            CreateRandomTextFile(baseDirectory.GetFile("TEST_1MBぐらいのファイル1.txt"), 1024 * 1024);
            CreateRandomTextFile(baseDirectory.GetFile("TEST_1MBぐらいのファイル2.txt"), 1024 * 1024);
            CreateRandomTextFile(baseDirectory.GetFile("TEST_1MBぐらいのファイル3.txt"), 1024 * 1024);
            CreateRandomTextFile(baseDirectory.GetFile("TEST_4GBよりやや短いファイル.txt"), UInt32.MaxValue - 100);
            CreateRandomTextFile(baseDirectory.GetFile("TEST_4GBを超えるが圧縮すると4GB未満のファイル.txt"), (Int64)UInt32.MaxValue + 1);
            CreateRandomTextFile(baseDirectory.GetFile("TEST_圧縮しても4GBを超えるファイル.txt"), (Int64)UInt32.MaxValue + (UInt32.MaxValue / 4));
        }

        private static void CreateRandomTextFile(FileInfo file, long length)
        {
            file.Delete();
            using (var outputStream = file.Create().AsOutputByteStream().WithCache().AsStream())
            using (var writer = new StreamWriter(outputStream, Encoding.UTF8))
            {
                var lockObject = new object();
                Enumerable.Range(0, Environment.ProcessorCount)
                    .AsParallel()
                    .ForAll(n =>
                    {
                        while (true)
                        {
                            var actualCount = 0;
                            lock (lockObject)
                            {
                                if (length <= 0)
                                    return;
                                actualCount = (int)length.Minimum(1024 * 1024);
                                length -= actualCount;
                            }
                            var data = RandomSequence.AsciiCharSequence.Take((int)actualCount).ToArray();
                            lock (lockObject)
                            {
                                writer.Write(data);
                            }
                        }
                    });
            }
        }
    }
}
