using System;
using System.IO;
using Utility;
using Utility.IO;

namespace Test.Utility.IO
{
    static class TestBitOutputStream
    {
        private static readonly FileInfo _testFile = new(Path.Combine(Path.GetDirectoryName(typeof(TestBitInputStream).Assembly.Location) ?? ".", "testData.txt"));

        public static void Test()
        {
            Test(BitPackingDirection.LsbToMsb);
            Test(BitPackingDirection.MsbToLsb);
        }

        private static void Test(BitPackingDirection bitPackingDirection)
        {
            using var ms = new MemoryStream();
            using (var sourceBitStream = _testFile.OpenRead().AsInputByteStream().AsBitStream(bitPackingDirection))
            using (var bitOutputStream = ms.AsOutputByteStream().AsBitStream(bitPackingDirection, true))
            {
                var bitCounts = new[] { 1, 3, 5, 7, 11 };
                var count = 0;
                while (true)
                {
                    var bitCount = bitCounts[count];
                    var bitArray = sourceBitStream.ReadBits(bitCount);
                    if (!bitArray.HasValue)
                        break;
                    bitOutputStream.Write(bitArray.Value);
                    count = (count + 1) % bitCounts.Length;
                }
            }
            ms.Position = 0;
            if (!ms.StreamBytesEqual(_testFile.OpenRead()))
                throw new Exception();
        }
    }
}
