using System;
using System.IO;
using System.Linq;
using System.Text;
using Utility;
using Utility.IO;

namespace Test.Utility.IO
{
    static class TestBitInputStream
    {
        public static void Test()
        {
            Test(BitPackingDirection.LsbToMsb);
            Test(BitPackingDirection.MsbToLsb);
        }

        private static void Test(BitPackingDirection bitPackingDirection)
        {
            var _testFile = new FileInfo(Path.Combine(Path.GetDirectoryName(typeof(TestBitInputStream).Assembly.Location) ?? ".", "testData.txt"));
            var buffer1 = new BitQueue();
            using (var bitInputStream = _testFile.OpenRead().AsInputByteStream().WithCache().AsBitStream(bitPackingDirection))
            {
                var bitCounts = new[] { 1, 3, 5, 7, 11 };
                var count = 0;
                while (true)
                {
                    var bitArray = bitInputStream.ReadBits(bitCounts[count]);
                    if (!bitArray.HasValue)
                        break;
                    buffer1.Enqueue(bitArray.Value);
                    count = (count + 1) % bitCounts.Length;
                }
            }
            var desiredBitPatternStringBuffer = new StringBuilder();
            using (var inputStream = _testFile.OpenRead().AsInputByteStream().WithCache())
            {
                while (true)
                {
                    var data = inputStream.ReadByteOrNull();
                    if (data is null)
                        break;
                    if (bitPackingDirection == BitPackingDirection.MsbToLsb)
                        desiredBitPatternStringBuffer.Append(Convert.ToString((Byte)data, 2).PadLeft(8, '0'));
                    else
                        desiredBitPatternStringBuffer.Append(new string(Convert.ToString((Byte)data, 2).PadLeft(8, '0').Reverse().ToArray()));
                }
            }
            if (!string.Equals(buffer1.ToString("R"), desiredBitPatternStringBuffer.ToString(), StringComparison.Ordinal))
                throw new Exception();
        }
    }
}
