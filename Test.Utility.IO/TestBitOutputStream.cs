using System;
using System.IO;
using Utility;
using Utility.IO;

namespace Test.Utility.IO
{
    static class TestBitOutputStream
    {
        private static FileInfo _testFile = new FileInfo(Path.Combine(Path.GetDirectoryName(typeof(TestBitInputStream).Assembly.Location), "testData.txt"));

        public static void Test()
        {
            Test(BitPackingDirection.LsbToMsb);
            Test(BitPackingDirection.MsbToLsb);
        }

        private static void Test(BitPackingDirection packingDirection)
        {
            using (var ms = new MemoryStream())
            {
                using (var sourceBitStream = _testFile.OpenRead().AsInputByteStream().AsBitStream(packingDirection))
                using (var bitOutputStream = ms.AsOutputByteStream().AsBitStream(packingDirection, true))
                {
                    var bitCounts = new[] { 1, 3, 5, 7, 11 };
                    var count = 0;
                    while (true)
                    {
                        var bitCount = bitCounts[count];
                        var bitArray = sourceBitStream.ReadBits(bitCount);
                        if (bitArray == null)
                            break;
                        bitOutputStream.Write(bitArray);
                        count = (count + 1) % bitCounts.Length;
                    }
                }
                ms.Position = 0;
                if (!ms.StreamBytesEqual(_testFile.OpenRead()))
                    throw new Exception();
            }
        }
    }
}
