using System;
using System.Linq;
using Utility;

namespace Test.Utility
{
    static class TestCopyMemory
    {
        public static void Test()
        {
            TestBoolean();
            TestChar();
            TestSByte();
            TestByte();
            TestInt16();
            TestUInt16();
            TestInt32();
            TestUInt32();
            TestInt64();
            TestUInt64();
            TestSingle();
            TestDouble();
            TestDecimal();
        }

        private static void TestBoolean()
        {
            TestLoop(RandomSequence.GetBooleanSequence().Take(10 * 1024).ToArray());
        }

        private static void TestChar()
        {
            TestLoop(RandomSequence.GetAsciiCharSequence().Take(10 * 1024).ToArray());
        }

        private static void TestSByte()
        {
            TestLoop(RandomSequence.GetSByteSequence().Take(10 * 1024).ToArray());
        }

        private static void TestByte()
        {
            TestLoop(RandomSequence.GetByteSequence().Take(10 * 1024).ToArray());
        }

        private static void TestInt16()
        {
            TestLoop(RandomSequence.GetInt16Sequence().Take(10 * 1024).ToArray());
        }

        private static void TestUInt16()
        {
            TestLoop(RandomSequence.GetUInt16Sequence().Take(10 * 1024).ToArray());
        }

        private static void TestInt32()
        {
            TestLoop(RandomSequence.GetInt32Sequence().Take(10 * 1024).ToArray());
        }

        private static void TestUInt32()
        {
            TestLoop(RandomSequence.GetUInt32Sequence().Take(10 * 1024).ToArray());
        }

        private static void TestInt64()
        {
            TestLoop(RandomSequence.GetInt64Sequence().Take(10 * 1024).ToArray());
        }

        private static void TestUInt64()
        {
            TestLoop(RandomSequence.GetUInt64Sequence().Take(10 * 1024).ToArray());
        }

        private static void TestSingle()
        {
            TestLoop(RandomSequence.GetSingleSequence().Take(10 * 1024).ToArray());
        }

        private static void TestDouble()
        {
            TestLoop(RandomSequence.GetDoubleSequence().Take(10 * 1024).ToArray());
        }

        private static void TestDecimal()
        {
            TestLoop(RandomSequence.GetDecimalSequence().Take(10 * 1024).ToArray());
        }

        private static void TestLoop<ELEMENT_T>(ELEMENT_T[] dataSource)
            where ELEMENT_T : unmanaged, IEquatable<ELEMENT_T>
        {
            var expectedArray = new ELEMENT_T[dataSource.Length];
            var actualArray = new ELEMENT_T[dataSource.Length];
            unsafe
            {
                fixed (ELEMENT_T* ptr1 = expectedArray)
                fixed (ELEMENT_T* ptr2 = actualArray)
                {
                    Enumerable.Range(0, 11)
                        .Select(shiftCount => 1 << shiftCount)
                        .SelectMany(length1 => Enumerable.Range(-8, 17).Where(length2 => length2 + length1 >= 0), (length1, length2) => length1 + length2)
                        .Distinct()
                        .SelectMany(length => Enumerable.Range(0, 8), (length, offset1) => (length, offset1))
                        .SelectMany(item => Enumerable.Range(-8, 17).Concat(Enumerable.Range(item.length - 8, 17)).Where(offset2 => item.offset1 + offset2 >= 0), (item, offset2) => new { item.length, item.offset1, offset2 = item.offset1 + offset2 })
                        .ForEach(item =>
                        {
                            TestCore(dataSource, expectedArray, actualArray, item.offset1, item.offset2, item.length);
                            TestCore(dataSource, expectedArray, actualArray, item.offset2, item.offset1, item.length);
                        });
                }
            }
        }

        private static void TestCore<ELEMENT_T>(ELEMENT_T[] dataSource, ELEMENT_T[] expectedArray, ELEMENT_T[] actualArray, Int32 sourceOffset, Int32 destinationOffset, Int32 length)
            where ELEMENT_T : unmanaged, IEquatable<ELEMENT_T>
        {
            dataSource.CopyTo(expectedArray, 0);
            for (var index = 0; index < length; ++index)
                expectedArray[destinationOffset + index] = expectedArray[sourceOffset + index];
            dataSource.CopyTo(actualArray, 0);
            actualArray.CopyMemoryTo(sourceOffset, actualArray, destinationOffset, length);
            if (!actualArray.SequenceEqual(expectedArray))
                Console.WriteLine($"TestCopyMemory.TestCore<{typeof(ELEMENT_T).FullName}>: 処理結果が一致しません。: sourceOffset={sourceOffset}, destinationOffset={destinationOffset}, length={length}");
        }
    }
}
