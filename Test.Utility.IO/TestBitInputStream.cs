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

        private static void Test(BitPackingDirection packingDirection)
        {
            var _testFile = new FileInfo(Path.Combine(Path.GetDirectoryName(typeof(TestBitInputStream).Assembly.Location), "testData.txt"));
            var buffer1 = new SerializedBitArray();
            using (var bitInputStream = new BitInputStream(new BufferedInputStream(_testFile.OpenRead(), false), packingDirection, false))
            {
                var bitCounts = new[] { 1, 3, 5, 7, 11 };
                var count = 0;
                while (true)
                {
                    var bitArray = bitInputStream.Read(bitCounts[count]);
                    if (bitArray == null)
                        break;
                    buffer1.Append(bitArray);
                    count = (count + 1) % bitCounts.Length;
                }
            }
            var desiredBitPatternStringBuffer = new StringBuilder();
            using (var inputStream = new BufferedInputStream(_testFile.OpenRead(), false))
            {
                while (true)
                {
                    var data = inputStream.ReadByte();
                    if (data < 0)
                        break;
                    if (packingDirection == BitPackingDirection.MsbToLsb)
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
