using System;
using System.Collections.Generic;
using System.Linq;
using Utility;

namespace Test.Utility
{
    static class TestReadOnlySerializedBitArray
    {
        public static void Test()
        {
            TestEmpty();
            TestFromBooleanSequence();
            TestFromByteSequence();
            TestToByteArray();
            TestFromInteger();
            TestToInteger();
            TestConcat();
            TestDivide();
            TestGetBitArraySequence();
            TestGetByteSequence();
        }

        private static void TestEmpty()
        {
            if (!(ReadOnlySerializedBitArray.Empty.ToString("R") == ""))
                Console.Write("TestReadOnlySerializedBitArray.Empty: 処理結果が一致しません。: pattern = 1");
        }

        private static void TestFromBooleanSequence()
        {
            new bool?[] { true, false, null }
                .SelectMany(v1 => new bool?[] { true, false, null }, (v1, v2) => new[] { v1, v2 })
                .SelectMany(list => new bool?[] { true, false, null }, (list, v) => list.Concat(new[] { v }).ToList())
                .Select(list => list.Where(v => v != null).Select(v => v.Value))
                .Select(list => new { parameter = list.ToArray(), desired = string.Concat(list.Select(v => v ? "1" : "0")) })
                .GroupBy(item => item.desired)
                .Select(g => new { desired = g.Key, parameter = g.First().parameter })
                .ForEach(item =>
                {
                    var actual1 = ReadOnlySerializedBitArray.FromBooleanSequence(item.parameter).ToString("R");
                    var actual2 = ReadOnlySerializedBitArray.FromBooleanSequence(item.parameter.ToList()).ToString("R");
                    if (string.Equals(actual1, item.desired, StringComparison.Ordinal) == false)
                        Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestFromBooleanSequence: 処理結果が一致しません。: sequence={{{0}}}, actual={1}, desired={2}", string.Join(", ", item.parameter), actual1, item.desired));
                    if (string.Equals(actual2, item.desired, StringComparison.Ordinal) == false)
                        Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestFromBooleanSequence: 処理結果が一致しません。: sequence={{{0}}}, actual={1}, desired={2}", string.Join(", ", item.parameter), actual2, item.desired));
                });
        }

        private static void TestFromByteSequence()
        {
            new byte?[] { 0, 1, 2, 3, 6, 0x80, 0x40, 0xc0, 0x60, 0xff, null }
                .SelectMany(v1 => new byte?[] { 0, 1, 2, 3, 6, 0x80, 0x40, 0xc0, 0x60, 0xff, null }, (v1, v2) => new[] { v1, v2 })
                .SelectMany(list => new byte?[] { 0, 1, 2, 3, 6, 0x80, 0x40, 0xc0, 0x60, 0xff, null }, (list, v) => list.Concat(new[] { v }).ToArray())
                .Select(list => list.Where(v => v != null).Select(v => v.Value).ToArray())
                .Select(list => new { list, text = string.Join(",", list) })
                .GroupBy(list => list.text)
                .Select(g => g.First().list)
                .SelectMany(list => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (list, direction) => new { list, direction })
                .Select(item => new
                {
                    item.list,
                    item.direction,
                    desired =
                        string.Concat(
                            item.list
                            .Select(v =>
                                item.direction == BitPackingDirection.MsbToLsb
                                ? Convert.ToString(v, 2).PadLeft(8, '0')
                                : new string(Convert.ToString(v, 2).PadLeft(8, '0').ToCharArray().Reverse().ToArray())))
                })
                .ForEach(item =>
                {
                    var actual1 = ReadOnlySerializedBitArray.FromByteSequence(item.list, item.direction).ToString("R");
                    var actual2 = ReadOnlySerializedBitArray.FromByteSequence(item.list.ToList(), item.direction).ToString("R");
                    if (string.Equals(actual1, item.desired, StringComparison.Ordinal) == false)
                        Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestFromByteSequence: 処理結果が一致しません。: sequence={{{0}}}, direction={1}, actual={2}, desired={3}", string.Join(", ", item.list.Select(v => string.Format("0x{0:x2}", v))), item.direction, actual1, item.desired));
                    if (string.Equals(actual2, item.desired, StringComparison.Ordinal) == false)
                        Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestFromByteSequence: 処理結果が一致しません。: sequence={{{0}}}, direction={1}, actual={2}, desired={3}", string.Join(", ", item.list.Select(v => string.Format("0x{0:x2}", v))), item.direction, actual2, item.desired));
                });
        }

        private static void TestFromInteger()
        {
            new byte[] { 0, 1, 2, 3, 6, 0x80, 0x40, 0xc0, 0x60, 0xff }
                .SelectMany(value => Enumerable.Range(1, 7), (value, count) => new { value, count })
                .SelectMany(item => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (item, direction) => new { item.value, item.count, direction })
                .Select(item => new
                {
                    item.value,
                    item.count,
                    item.direction,
                    desired =
                        item.direction == BitPackingDirection.MsbToLsb
                        ? Convert.ToString(item.value, 2).PadLeft(8, '0').Substring(8 - item.count)
                        : new string(Convert.ToString(item.value, 2).PadLeft(8, '0').Substring(8 - item.count).ToCharArray().Reverse().ToArray())
                })
                .ForEach(item =>
                {
                    var actual = ReadOnlySerializedBitArray.FromByte(item.value, item.count, item.direction).ToString("R");
                    if (string.Equals(actual, item.desired, StringComparison.Ordinal) == false)
                        Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestFromInteger.FromByte: 処理結果が一致しません。: value=0x{0:x2}, bitCount={1}, direction={2}, actual={3}, desired={4}", item.value, item.count, item.direction, actual, item.desired));
                });

            new UInt16[] { 0, 1, 2, 3, 6, 0x8000, 0x4000, 0xc000, 0x6000, 0xffff }
                .SelectMany(value => Enumerable.Range(1, 15), (value, count) => new { value, count })
                .SelectMany(item => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (item, direction) => new { item.value, item.count, direction })
                .Select(item => new
                {
                    item.value,
                    item.count,
                    item.direction,
                    desired =
                        item.direction == BitPackingDirection.MsbToLsb
                        ? Convert.ToString(item.value, 2).PadLeft(16, '0').Substring(16 - item.count)
                        : new string(Convert.ToString(item.value, 2).PadLeft(16, '0').Substring(16 - item.count).ToCharArray().Reverse().ToArray())
                })
                .ForEach(item =>
                {
                    var actual = ReadOnlySerializedBitArray.FromUInt16(item.value, item.count, item.direction).ToString("R");
                    if (string.Equals(actual, item.desired, StringComparison.Ordinal) == false)
                        Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestFromInteger.FromUint16: 処理結果が一致しません。: value=0x{0:x4}, bitCount={1}, direction={2}, actual={3}, desired={4}", item.value, item.count, item.direction, actual, item.desired));
                });

            new UInt32[] { 0, 1, 2, 3, 6, 0x80000000, 0x40000000, 0xc0000000, 0x60000000, 0xffffffff }
                .SelectMany(value => Enumerable.Range(1, 31), (value, count) => new { value, count })
                .SelectMany(item => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (item, direction) => new { item.value, item.count, direction })
                .Select(item => new
                {
                    item.value,
                    item.count,
                    item.direction,
                    desired =
                        item.direction == BitPackingDirection.MsbToLsb
                        ? Convert.ToString(item.value, 2).PadLeft(32, '0').Substring(32 - item.count)
                        : new string(Convert.ToString(item.value, 2).PadLeft(32, '0').Substring(32 - item.count).ToCharArray().Reverse().ToArray())
                })
                .ForEach(item =>
                {
                    var actual = ReadOnlySerializedBitArray.FromUInt32(item.value, item.count, item.direction).ToString("R");
                    if (string.Equals(actual, item.desired, StringComparison.Ordinal) == false)
                        Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestFromInteger.FromUInt32: 処理結果が一致しません。: value=0x{0:x8}, bitCount={1}, direction={2}, actual={3}, desired={4}", item.value, item.count, item.direction, actual, item.desired));
                });

            new UInt64[] { 0, 1, 2, 3, 6, 0x8000000000000000, 0x4000000000000000, 0xc000000000000000, 0x6000000000000000, 0xffffffffffffffff }
                .SelectMany(value => Enumerable.Range(1, 63), (value, count) => new { value, count })
                .SelectMany(item => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (item, direction) => new { item.value, item.count, direction })
                .Select(item => new
                {
                    item.value,
                    item.count,
                    item.direction,
                    desired =
                        item.direction == BitPackingDirection.MsbToLsb
                        ? Convert.ToString((long)item.value, 2).PadLeft(64, '0').Substring(64 - item.count)
                        : new string(Convert.ToString((long)item.value, 2).PadLeft(64, '0').Substring(64 - item.count).ToCharArray().Reverse().ToArray())
                })
                .ForEach(item =>
                {
                    var actual = ReadOnlySerializedBitArray.FromUInt64(item.value, item.count, item.direction).ToString("R");
                    if (string.Equals(actual, item.desired, StringComparison.Ordinal) == false)
                        Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestFromInteger.FromUInt64: 処理結果が一致しません。: value=0x{0:x16}, bitCount={1}, direction={2}, actual={3}, desired={4}", item.value, item.count, item.direction, actual, item.desired));
                });
        }

        private static void TestToByteArray()
        {
            var lockObject = new object();

            BitPatternDataSource
                .QuickDistinct(StringComparer.Ordinal)
                .SelectMany(bitPattern => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (bitPattern, direction) => new { bitPattern, direction })
                .Select(item => new
                {
                    bitArray = SerializedBitArray.Parse(item.bitPattern),
                    direction = item.direction,
                    desired =
                        item.bitPattern
                        .ToChunkOfArray(8)
                        .Select(array => item.direction == BitPackingDirection.MsbToLsb ? array : array.Reverse().ToArray())
                        .Select(array => Convert.ToByte(new string(array), 2)),
                })
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(item =>
                {
                    var actual = item.bitArray.ToByteArray(item.direction);
                    if (!actual.SequenceEqual(item.desired))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(
                                string.Format(
                                    "TestReadOnlySerializedBitArray.TestToByteArray.ToByteArray: 処理結果が一致しません。: bitArray={0}, direction={1}, actual={{{2}}}, desired={{{3}}}",
                                    item.bitArray,
                                    item.direction,
                                    string.Join(
                                        ", ",
                                        actual.Select(value => string.Format("0x{0:x2}", value))),
                                    string.Join(
                                        ", ",
                                        item.desired.Select(value => string.Format("0x{0:x2}", value)))));
                        }
                    }
                });
        }

        private static void TestToInteger()
        {
            new Byte[] { 0, 1, 2, 3, 6, 0x80, 0x40, 0xc0, 0x60, 0xff }
                .SelectMany(value => Enumerable.Range(1, 7), (value, count) => new { value, count })
                .SelectMany(item => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (item, direction) => new { item.value, item.count, direction })
                .Select(item => new
                {
                    bitArray = ReadOnlySerializedBitArray.FromByte(item.value, item.count, item.direction),
                    item.direction,
                    desired = (Byte)(item.value & ((1 << item.count) - 1)),
                })
                .ForEach(item =>
                {
                    var actual = item.bitArray.ToByte(item.direction);
                    if (actual != item.desired)
                        Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestToInteger.ToByte: 処理結果が一致しません。: bitArray={0}, direction={1}, actual={2:x2}, desired={3:x2}", item.bitArray, item.direction, actual, item.desired));
                });

            new UInt16[] { 0, 1, 2, 3, 6, 0x8000, 0x4000, 0xc000, 0x6000, 0xffff }
                .SelectMany(value => Enumerable.Range(1, 15), (value, count) => new { value, count })
                .SelectMany(item => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (item, direction) => new { item.value, item.count, direction })
                .Select(item => new
                {
                    bitArray = ReadOnlySerializedBitArray.FromUInt16(item.value, item.count, item.direction),
                    item.direction,
                    desired = (UInt16)(item.value & ((1 << item.count) - 1)),
                })
                .ForEach(item =>
                {
                    var actual = item.bitArray.ToUInt16(item.direction);
                    if (actual != item.desired)
                        Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestToInteger.ToUint16: 処理結果が一致しません。: bitArray={0}, direction={1}, actual={2:x4}, desired={3:x4}", item.bitArray, item.direction, actual, item.desired));
                });

            new UInt32[] { 0, 1, 2, 3, 6, 0x80000000, 0x40000000, 0xc0000000, 0x60000000, 0xffffffff }
                .SelectMany(value => Enumerable.Range(1, 31), (value, count) => new { value, count })
                .SelectMany(item => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (item, direction) => new { item.value, item.count, direction })
                .Select(item => new
                {
                    bitArray = ReadOnlySerializedBitArray.FromUInt32(item.value, item.count, item.direction),
                    item.direction,
                    desired = item.value & ((1U << item.count) - 1),
                })
                .ForEach(item =>
                {
                    var actual = item.bitArray.ToUInt32(item.direction);
                    if (actual != item.desired)
                        Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestToInteger.ToUInt32: 処理結果が一致しません。: bitArray={0}, direction={1}, actual={2:x8}, desired={3:x8}", item.bitArray, item.direction, actual, item.desired));
                });

            new UInt64[] { 0, 1, 2, 3, 6, 0x8000000000000000, 0x4000000000000000, 0xc000000000000000, 0x6000000000000000, 0xffffffffffffffff }
                .SelectMany(value => Enumerable.Range(1, 63), (value, count) => new { value, count })
                .SelectMany(item => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (item, direction) => new { item.value, item.count, direction })
                .Select(item => new
                {
                    bitArray = ReadOnlySerializedBitArray.FromUInt64(item.value, item.count, item.direction),
                    item.direction,
                    desired = item.value & ((1UL << item.count) - 1),
                })
                .ForEach(item =>
                {
                    var actual = item.bitArray.ToUInt64(item.direction);
                    if (actual != item.desired)
                        Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestToInteger.ToUInt64: 処理結果が一致しません。: bitArray={0}, direction={1}, actual={2:x16}, desired={3:x16}", item.bitArray, item.direction, actual, item.desired));
                });
        }

        private static void TestConcat()
        {
            var lockObject = new object();

            BitPatternDataSource
                .QuickDistinct(StringComparer.Ordinal)
                .SelectMany(bitPattern1 => BitPatternDataSource.QuickDistinct(StringComparer.Ordinal), (bitPattern1, bitPattern2) => new { bitPattern1, bitPattern2 })
                .Select(item => new
                {
                    bitArray = ReadOnlySerializedBitArray.Parse(item.bitPattern1),
                    sequence = item.bitPattern2.ToCharArray().Select(c => c == '1'),
                    desired = item.bitPattern1 + item.bitPattern2,
                })
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(item =>
                {
                    var actual1 = item.bitArray.Concat(item.sequence).ToString("R");
                    var actual2 = item.bitArray.Concat(item.sequence.ToArray()).ToString("R");
                    var actual3 = item.bitArray.Concat(item.sequence.ToArray().AsReadOnly()).ToString("R");
                    if (!string.Equals(actual1, item.desired, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestConcat.Concat(IEnumerable<bool>): 処理結果が一致しません。: bitArray={0}, sequence={{{1}}}, actual={2}, desired={3}", item.bitArray, string.Join(",", item.sequence.Select(b => b ? "1" : "0")), actual1, item.desired));
                        }
                    }
                    if (!string.Equals(actual2, item.desired, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestConcat.Concat(bool[]): 処理結果が一致しません。: bitArray={0}, sequence={{{1}}}, actual={2}, desired={3}", item.bitArray, string.Join(",", item.sequence.Select(b => b ? "1" : "0")), actual2, item.desired));
                        }
                    }
                    if (!string.Equals(actual3, item.desired, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestConcat.Concat(IReadOnlyArray<bool>): 処理結果が一致しません。: bitArray={0}, sequence={{{1}}} actual={2}, desired={3}", item.bitArray, string.Join(",", item.sequence.Select(b => b ? "1" : "0")), actual3, item.desired));
                        }
                    }
                });

            BitPatternDataSource
                .QuickDistinct(StringComparer.Ordinal)
                .SelectMany(bitPattern1 => BitPatternDataSource.Where(s => s.Length % 8 == 0).QuickDistinct(StringComparer.Ordinal), (bitPattern1, bitPattern2) => new { bitPattern1, bitPattern2 = bitPattern2.ToChunkOfArray(8).Select(array => new string(array)) })
                .SelectMany(item => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (item, direction) => new { item.bitPattern1, item.bitPattern2, direction })
                .Select(item => new
                {
                    bitArray = ReadOnlySerializedBitArray.Parse(item.bitPattern1),
                    sequence =
                        item.bitPattern2
                        .Select(pattern =>
                                item.direction == BitPackingDirection.MsbToLsb
                                ? Convert.ToByte(pattern, 2)
                                : Convert.ToByte(new string(pattern.Reverse().ToArray()), 2)),
                    item.direction,
                    desired = item.bitPattern1 + string.Concat(item.bitPattern2),
                })
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(item =>
                {
                    var actual1 = item.bitArray.Concat(item.sequence, item.direction).ToString("R");
                    var actual2 = item.bitArray.Concat(item.sequence.ToArray(), item.direction).ToString("R");
                    var actual3 = item.bitArray.Concat(item.sequence.ToArray().AsReadOnly(), item.direction).ToString("R");
                    if (!string.Equals(actual1, item.desired, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestConcat.Concat(IEnumerable<byte>): 処理結果が一致しません。: bitArray={0}, sequence={{{1}}}, direction={2}, actual={3}, desired={4}", item.bitArray, item.sequence.Select(b => string.Format("0x{0:x2}", b)), item.direction, actual1, item.desired));
                        }
                    }
                    if (!string.Equals(actual2, item.desired, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestConcat.Concat(byte[]): 処理結果が一致しません。: bitArray={0}, sequence={{{1}}}, direction={2}, actual={3}, desired={4}", item.bitArray, item.sequence.Select(b => string.Format("0x{0:x2}", b)), item.direction, actual2, item.desired));
                        }
                    }
                    if (!string.Equals(actual3, item.desired, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestConcat.Concat(IReadOnlyArray<byte>): 処理結果が一致しません。: bitArray={0}, sequence={{{1}}}, direction={2}, actual={3}, desired={4}", item.bitArray, item.sequence.Select(b => string.Format("0x{0:x2}", b)), item.direction, actual3, item.desired));
                        }
                    }
                });

            BitPatternDataSource
                .QuickDistinct(StringComparer.Ordinal)
                .SelectMany(bitPattern1 => BitPatternDataSource.Where(s => s.Length == 8).QuickDistinct(StringComparer.Ordinal), (bitPattern1, bitPattern2) => new { bitPattern1, bitPattern2 })
                .SelectMany(item => BitCountDataSource.Where(count => count.IsBetween(1, 8)), (item, count) => new { item.bitPattern1, item.bitPattern2, count })
                .SelectMany(item => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (item, direction) => new { item.bitPattern1, item.bitPattern2, item.count, direction })
                .Select(item => new
                {
                    bitArray = ReadOnlySerializedBitArray.Parse(item.bitPattern1),
                    value = Convert.ToByte(item.bitPattern2, 2),
                    count = item.count,
                    item.direction,
                    desired = item.bitPattern1 +
                        (item.direction == BitPackingDirection.MsbToLsb
                            ? item.bitPattern2.PadLeft(8, '0').Substring(8 - item.count)
                            : new string(item.bitPattern2.PadLeft(8, '0').Substring(8 - item.count).Reverse().ToArray())),
                })
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(item =>
                {
                    var actual = item.bitArray.Concat(item.value, item.count, item.direction).ToString("R");
                    if (!string.Equals(actual, item.desired, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestConcat.Concat(byte): 処理結果が一致しません。: bitArray={0}, value=0x{1:x2}, count={2}, direction={3}, actual={4}, desired={5}", item.bitArray, item.value, item.count, item.direction, actual, item.desired));
                        }
                    }
                });

            BitPatternDataSource
                .QuickDistinct(StringComparer.Ordinal)
                .SelectMany(bitPattern1 => BitPatternDataSource.Where(s => s.Length == 16).QuickDistinct(StringComparer.Ordinal), (bitPattern1, bitPattern2) => new { bitPattern1, bitPattern2 })
                .SelectMany(item => BitCountDataSource.Where(count => count.IsBetween(1, 16)), (item, count) => new { item.bitPattern1, item.bitPattern2, count })
                .SelectMany(item => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (item, direction) => new { item.bitPattern1, item.bitPattern2, item.count, direction })
                .Select(item => new
                {
                    bitArray = ReadOnlySerializedBitArray.Parse(item.bitPattern1),
                    value = Convert.ToUInt16(item.bitPattern2, 2),
                    count = item.count,
                    item.direction,
                    desired = item.bitPattern1 +
                        (item.direction == BitPackingDirection.MsbToLsb
                            ? item.bitPattern2.PadLeft(16, '0').Substring(16 - item.count)
                            : new string(item.bitPattern2.PadLeft(16, '0').Substring(16 - item.count).Reverse().ToArray())),
                })
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(item =>
                {
                    var actual = item.bitArray.Concat(item.value, item.count, item.direction).ToString("R");
                    if (!string.Equals(actual, item.desired, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestConcat.Concat(UInt16): 処理結果が一致しません。: bitArray={0}, value=0x{1:x4}, count={2}, direction={3}, actual={4}, desired={5}", item.bitArray, item.value, item.count, item.direction, actual, item.desired));
                        }
                    }
                });

            BitPatternDataSource
                .QuickDistinct(StringComparer.Ordinal)
                .SelectMany(bitPattern1 => BitPatternDataSource.Where(s => s.Length == 32).QuickDistinct(StringComparer.Ordinal), (bitPattern1, bitPattern2) => new { bitPattern1, bitPattern2 })
                .SelectMany(item => BitCountDataSource.Where(count => count.IsBetween(1, 32)), (item, count) => new { item.bitPattern1, item.bitPattern2, count })
                .SelectMany(item => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (item, direction) => new { item.bitPattern1, item.bitPattern2, item.count, direction })
                .Select(item => new
                {
                    bitArray = ReadOnlySerializedBitArray.Parse(item.bitPattern1),
                    value = Convert.ToUInt32(item.bitPattern2, 2),
                    count = item.count,
                    item.direction,
                    desired = item.bitPattern1 +
                        (item.direction == BitPackingDirection.MsbToLsb
                            ? item.bitPattern2.PadLeft(32, '0').Substring(32 - item.count)
                            : new string(item.bitPattern2.PadLeft(32, '0').Substring(32 - item.count).Reverse().ToArray())),
                })
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(item =>
                {
                    var actual = item.bitArray.Concat(item.value, item.count, item.direction).ToString("R");
                    if (!string.Equals(actual, item.desired, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestConcat.Concat(UInt32): 処理結果が一致しません。: bitArray={0}, value=0x{1:x8}, count={2}, direction={3}, actual={4}, desired={5}", item.bitArray, item.value, item.count, item.direction, actual, item.desired));
                        }
                    }
                });

            BitPatternDataSource
                .QuickDistinct(StringComparer.Ordinal)
                .SelectMany(bitPattern1 => BitPatternDataSource.Where(s => s.Length == 64).QuickDistinct(StringComparer.Ordinal), (bitPattern1, bitPattern2) => new { bitPattern1, bitPattern2 })
                .SelectMany(item => BitCountDataSource.Where(count => count.IsBetween(1, 64)), (item, count) => new { item.bitPattern1, item.bitPattern2, count })
                .SelectMany(item => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (item, direction) => new { item.bitPattern1, item.bitPattern2, item.count, direction })
                .Select(item => new
                {
                    bitArray = ReadOnlySerializedBitArray.Parse(item.bitPattern1),
                    value = Convert.ToUInt64(item.bitPattern2, 2),
                    count = item.count,
                    item.direction,
                    desired = item.bitPattern1 +
                        (item.direction == BitPackingDirection.MsbToLsb
                            ? item.bitPattern2.PadLeft(64, '0').Substring(64 - item.count)
                            : new string(item.bitPattern2.PadLeft(64, '0').Substring(64 - item.count).Reverse().ToArray())),
                })
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(item =>
                {
                    var actual = item.bitArray.Concat(item.value, item.count, item.direction).ToString("R");
                    if (!string.Equals(actual, item.desired, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestConcat.Concat(UInt64): 処理結果が一致しません。: bitArray={0}, value=0x{1:x16}, count={2}, direction={3}, actual={4}, desired={5}", item.bitArray, item.value, item.count, item.direction, actual, item.desired));
                        }
                    }
                });

            BitPatternDataSource
                .QuickDistinct(StringComparer.Ordinal)
                .SelectMany(bitPattern1 => BitPatternDataSource.QuickDistinct(StringComparer.Ordinal), (bitPattern1, bitPattern2) => new { bitPattern1, bitPattern2 })
                .Select(item => new
                {
                    bitArray1 = ReadOnlySerializedBitArray.Parse(item.bitPattern1),
                    bitArray2 = ReadOnlySerializedBitArray.Parse(item.bitPattern2),
                    desired = item.bitPattern1 + item.bitPattern2,
                })
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(item =>
                {
                    var actual1 = item.bitArray1.Concat(item.bitArray2).ToString("R");
                    if (!string.Equals(actual1, item.desired, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestConcat.Concat(ReadOnlybitArray): 処理結果が一致しません。: bitArray1={0}, bitArray2={1}, actual={2}, desired={3}", item.bitArray1, item.bitArray2, actual1, item.desired));
                        }
                    }
                });
        }

        private static void TestDivide()
        {
            var lockObject = new object();

            BitPatternDataSource
                .QuickDistinct(StringComparer.Ordinal)
                .SelectMany(bitPattern => BitCountDataSource.Where(count => count.IsBetween(0, bitPattern.Length)), (bitPattern, count) => new { bitPattern, count })
                .Select(item => new
                {
                    bitArray = ReadOnlySerializedBitArray.Parse(item.bitPattern),
                    count = item.count,
                    desired1 = item.bitPattern.Substring(0, item.count),
                    desired2 = item.bitPattern.Substring(item.count),
                })
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(item =>
                {
                    ReadOnlySerializedBitArray remain;
                    var actual1 = item.bitArray.Divide(item.count, out remain).ToString("R");
                    var actual2 = remain.ToString("R");
                    if (!string.Equals(actual1, item.desired1, StringComparison.Ordinal) || !string.Equals(actual2, item.desired2, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestConcat.Divide: 処理結果が一致しません。: bitArray={0}, count={1}, actual1={2}, actual2={3}, desired1={4}, desired2={5}", item.bitArray, item.count, actual1, actual2, item.desired1, item.desired2));
                        }
                    }
                });
        }

        private static void TestGetBitArraySequence()
        {
            var lockObject = new object();

            BitPatternDataSource
                .SelectMany(bitPattern1 => BitPatternDataSource, (bitPattern1, bitPattern2) => bitPattern1 + bitPattern2)
                .Where(bitPattern => bitPattern.Length % 8 == 0)
                .QuickDistinct(StringComparer.Ordinal)
                .SelectMany(bitPattern => BitCountDataSource.Where(bitCount => bitCount > 0), (bitPattern, bitCount) => new { bitPattern = bitPattern, bitCount })
                .SelectMany(item => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (item, direction) => new { bitPatterns = item.bitPattern.ToChunkOfArray(8).ToArray(), item.bitCount, direction })
                .Select(item => new
                {
                    byteArray = item.bitPatterns.Select(bitPattern => Convert.ToByte(new string(bitPattern), 2)),
                    item.bitCount,
                    item.direction,
                    desired =
                        string.Join(
                             "-",
                            item.bitPatterns
                            .SelectMany(bitPattern =>
                                item.direction == BitPackingDirection.MsbToLsb
                                ? bitPattern
                                : bitPattern.Reverse().ToArray())
                            .ToChunkOfArray(item.bitCount)
                            .Select(element => new string(element))),
                })
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(item =>
                {
                    var bitArrays = item.byteArray.GetBitArraySequence(item.bitCount, item.direction);
                    var actual = string.Join("-", bitArrays.Select(bitArray => bitArray.ToString("R")));
                    if (!string.Equals(actual, item.desired, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestGetBitArraySequence.GetBitArraySequence<IEnumerable<byte>>: 処理結果が一致しません。: bitArray={0}, bitCount={1}, direction={2}, actual={3}, desired={4}", BitConverter.ToString(item.byteArray.ToArray()), item.bitCount, item.direction, actual, item.desired));
                        }
                    }
                });

            BitPatternDataSource
                .SelectMany(bitPattern1 => BitPatternDataSource, (bitPattern1, bitPattern2) => bitPattern1 + bitPattern2)
                .Where(bitPattern => bitPattern.Length % 8 == 0)
                .QuickDistinct(StringComparer.Ordinal)
                .SelectMany(bitPattern => new[] { 1, 2 }, (bitPattern, byteCount) => new { bitPattern, byteCount })
                .SelectMany(item => BitCountDataSource.Where(bitCount => bitCount > 0), (item, bitCount) => new { item.bitPattern, item.byteCount, bitCount })
                .SelectMany(item => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (item, direction) => new { bitPatterns = item.bitPattern.ToChunkOfArray(8).ToArray(), item.byteCount, item.bitCount, direction })
                .Select(item => new
                {
                    byteArray = item.bitPatterns.Select(bitPattern => Convert.ToByte(new string(bitPattern), 2)).ToChunkOfArray(item.byteCount),
                    item.bitCount,
                    item.direction,
                    desired =
                        string.Join(
                             "-",
                            item.bitPatterns
                            .SelectMany(bitPattern =>
                                item.direction == BitPackingDirection.MsbToLsb
                                ? bitPattern
                                : bitPattern.Reverse().ToArray())
                            .ToChunkOfArray(item.bitCount)
                            .Select(element => new string(element))),
                })
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(item =>
                {
                    var bitArrays = item.byteArray.GetBitArraySequence(item.bitCount, item.direction);
                    var actual = string.Join("-", bitArrays.Select(bitArray => bitArray.ToString("R")));
                    if (!string.Equals(actual, item.desired, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestReadOnlySerializedBitArray.TestGetBitArraySequence.GetBitArraySequence<IEnumerable<byte[]>>: 処理結果が一致しません。: bitArray={{{0}}}, bitCount={1}, direction={2}, actual={3}, desired={4}", string.Join(", ", item.byteArray.Select(array => BitConverter.ToString(array))), item.bitCount, item.direction, actual, item.desired));
                        }
                    }
                });
        }

        private static void TestGetByteSequence()
        {
            var lockObject = new object();

            BitPatternDataSource
                .SelectMany(bitPattern1 => BitPatternDataSource.Concat(new[] { (string)null }), (bitPattern1, bitPattern2) => string.Join("-", new[] { bitPattern1, bitPattern2 }.Where(bitPattern => bitPattern != null)))
                .SelectMany(bitPattern1 => BitPatternDataSource.Concat(new[] { (string)null }), (bitPattern1, bitPattern2) => string.Join("-", new[] { bitPattern1, bitPattern2 }.Where(bitPattern => bitPattern != null)))
                .QuickDistinct(StringComparer.Ordinal)
                .SelectMany(bitPatterns => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (bitPatterns, direction) => new { bitPatterns, direction })
                .Select(item => new
                {
                    bitArrays = item.bitPatterns.Split('-').Select(bitPattern => ReadOnlySerializedBitArray.Parse(bitPattern)),
                    item.direction,
                    desired =
                        item.bitPatterns
                        .Where(c => c != '-')
                        .ToChunkOfArray(8)
                        .Select(charArray =>
                            new string(
                                item.direction == BitPackingDirection.MsbToLsb
                                ? charArray
                                : charArray.Reverse().ToArray()))
                        .Select(bitPattern => Convert.ToByte(bitPattern, 2)),
                })
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(item =>
                {
                    var actual = item.bitArrays.GetByteSequence(item.direction).ToReadOnlyCollection();
                    if (!actual.SequenceEqual(item.desired))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(
                                string.Format(
                                    "TestReadOnlySerializedBitArray.TestGetByteSequence.GetByteSequence: 処理結果が一致しません。: bitArray={{{0}}}, direction={1}, actual={{{2}}}, desired={{{3}}}",
                                    string.Join(", ", item.bitArrays),
                                    item.direction,
                                    string.Join(", ", actual.Select(value => string.Format("0x{0:x2}", value)),
                                    string.Join(", ", item.desired.Select(value => string.Format("0x{0:x2}", value))))));
                        }
                    }
                });
        }

        private static IEnumerable<int> BitCountDataSource =>
            new[] { 0, 1, 2, 6, 7, 8, 9, 10, 14, 15, 16, 17, 18, 30, 31, 32, 33, 34, 62, 63, 64, 65, 66, 126, 127, 128, 129, 130 };

        private static IEnumerable<string> BitPatternDataSource
        {
            get
            {
                return new[]
                {
                    new string(Enumerable.Repeat('1', 64 * 3).ToArray()),
                    new string(Enumerable.Repeat('0', 64 * 3).ToArray()),
                    string.Concat(Enumerable.Repeat("00000001", 8 * 3)),
                    string.Concat(Enumerable.Repeat("10000000", 8 * 3)),
                    string.Concat(Enumerable.Repeat("11111110", 8 * 3)),
                    string.Concat(Enumerable.Repeat("01111111", 8 * 3)),
                    "0" + new string(Enumerable.Repeat('1', 64 * 3 - 1).ToArray()),
                    "1" + new string(Enumerable.Repeat('0', 64 * 3 - 1).ToArray()),
                    new string(Enumerable.Repeat('1', 64 * 3 - 1).ToArray()) + "0",
                    new string(Enumerable.Repeat('0', 64 * 3 - 1).ToArray()) + "1",
                    string.Concat(Enumerable.Repeat("10110011100011110000111000110010", 6)),
                    string.Concat(Enumerable.Repeat("01001100011100001111000111001101", 6)),
                }
                .SelectMany(
                    bitArray => BitCountDataSource,
                    (bitArray, length) => bitArray.Substring(0, length));
            }
        }
    }
}

