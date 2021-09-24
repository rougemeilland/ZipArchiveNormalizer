using System;
using System.Collections.Generic;
using System.Linq;
using Utility;

namespace Test.Utility
{
    static class TestBitQueue
    {
        public static void Test()
        {
            TestEnqueue();
            TestDequeue();
        }

        private static void TestEnqueue()
        {
            var lockObject = new object();

            BitPatternDataSource
                .QuickDistinct(StringComparer.Ordinal)
                .SelectMany(bitPattern => new[] { false, true }, (bitPattern, bitValue) => new { bitPattern, bitValue })
                .Select(item => new
                {
                    bitQueue = new BitQueue(item.bitPattern),
                    value = item.bitValue,
                    desired = item.bitPattern + (item.bitValue ? "1" : "0"),
                })
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(item =>
                {

                    var bitQueue = item.bitQueue.Clone();
                    bitQueue.Enqueue(item.value);
                    var actual = bitQueue.ToString("R");

                    if (!string.Equals(actual, item.desired, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestBitQueue.TestEnqueue.Enqueue(bool): 処理結果が一致しません。: bitQueue={0}, value={1}, actual={2}, desired={3}", item.bitQueue, item.value, actual, item.desired));
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
                    bitQueue = new BitQueue(item.bitPattern1),
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

                    var bitQueue = item.bitQueue.Clone();
                    bitQueue.Enqueue(item.value, item.count, item.direction);
                    var actual = bitQueue.ToString("R");

                    if (!string.Equals(actual, item.desired, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestBitQueue.TestEnqueue.Enqueue(byte): 処理結果が一致しません。: bitQueue={0}, value=0x{1:x2}, count={2}, direction={3}, actual={4}, desired={5}", item.bitQueue, item.value, item.count, item.direction, actual, item.desired));
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
                    bitQueue = new BitQueue(item.bitPattern1),
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
                    var bitQueue = item.bitQueue.Clone();
                    bitQueue.Enqueue(item.value, item.count, item.direction);
                    var actual = bitQueue.ToString("R");

                    if (!string.Equals(actual, item.desired, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestBitQueue.TestEnqueue.Enqueue(UInt16): 処理結果が一致しません。: bitQueue={0}, value=0x{1:x4}, count={2}, direction={3}, actual={4}, desired={5}", item.bitQueue, item.value, item.count, item.direction, actual, item.desired));
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
                    bitQueue = new BitQueue(item.bitPattern1),
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
                    var bitQueue = item.bitQueue.Clone();
                    bitQueue.Enqueue(item.value, item.count, item.direction);
                    var actual = bitQueue.ToString("R");

                    if (!string.Equals(actual, item.desired, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestBitQueue.TestEnqueue.Enqueue(UInt32): 処理結果が一致しません。: bitQueue={0}, value=0x{1:x8}, count={2}, direction={3}, actual={4}, desired={5}", item.bitQueue, item.value, item.count, item.direction, actual, item.desired));
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
                    bitQueue = new BitQueue(item.bitPattern1),
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
                    var bitQueue = item.bitQueue.Clone();
                    bitQueue.Enqueue(item.value, item.count, item.direction);
                    var actual = bitQueue.ToString("R");

                    if (!string.Equals(actual, item.desired, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestBitQueue.TestEnqueue.Enqueue(UInt64): 処理結果が一致しません。: bitQueue={0}, value=0x{1:x16}, count={2}, direction={3}, actual={4}, desired={5}", item.bitQueue, item.value, item.count, item.direction, actual, item.desired));
                        }
                    }
                });

            BitPatternDataSource
                .QuickDistinct(StringComparer.Ordinal)
                .SelectMany(bitPattern1 => BitPatternDataSource.QuickDistinct(StringComparer.Ordinal), (bitPattern1, bitPattern2) => new { bitPattern1, bitPattern2 })
                .Select(item => new
                {
                    bitQueue = new BitQueue(item.bitPattern1),
                    bitArray = new TinyBitArray(item.bitPattern2),
                    desired = item.bitPattern1 + item.bitPattern2,
                })
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(item =>
                {
                    var bitQueue = item.bitQueue.Clone();
                    bitQueue.Enqueue(item.bitArray);
                    var actual = bitQueue.ToString("R");

                    if (!string.Equals(actual, item.desired, StringComparison.Ordinal))
                    {
                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("TestBitQueue.TestEnqueue.Enqueue(bitArray): 処理結果が一致しません。: bitQueue={0}, bitArray={1}, actual={2}, desired={3}", item.bitQueue, item.bitArray, actual, item.desired));
                        }
                    }
                });
        }

        private static void TestDequeue()
        {
            BitPatternDataSource
                .QuickDistinct(StringComparer.Ordinal)
                .Where(bitPattern => bitPattern.Length >= 1)
                .Select(bitPattern => new
                {
                    bitQueue = new BitQueue(bitPattern),
                    desiredValue = bitPattern[0] != '0', 
                    desiredBitQueue = bitPattern.Substring(1),
                })
                .ForEach(item =>
                {
                    var bitQueue = item.bitQueue.Clone();
                    var actualValue = bitQueue.DequeueBoolean();
                    var actualBitQueue = bitQueue.ToString("R");
                    if (!(actualValue == item.desiredValue && string.Equals(actualBitQueue, item.desiredBitQueue, StringComparison.Ordinal)))
                        Console.WriteLine(string.Format("TestBitQueue.TestDequeue.DequeueBoolean: 処理結果が一致しません。: bitQueue={0}, actualValue={1}, actualBitQueue={2}, desiredValue={3}, desiredBitQueue={4}", item.bitQueue, actualValue, actualBitQueue, item.desiredValue, item.desiredBitQueue));
                });

            BitPatternDataSource
                .QuickDistinct(StringComparer.Ordinal)
                .Where(bitPattern => bitPattern.Length > 0)
                .SelectMany(bitPattern => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (bitPattern, direction) => new { bitPattern, direction })
                .Select(item => new
                {
                    bitQueue = new BitQueue(item.bitPattern),
                    item.direction,
                    desiredValue =
                        Convert.ToByte(
                            item.direction == BitPackingDirection.MsbToLsb
                            ? item.bitPattern.Length > 8 ? item.bitPattern.Substring(0, 8) : item.bitPattern
                            : item.bitPattern.Length > 8 ? new string(item.bitPattern.Substring(0, 8).Reverse().ToArray()): new string(item.bitPattern.Reverse().ToArray()),
                            2),
                    desiredBitQueue = item.bitPattern.Length > 8 ? item.bitPattern.Substring(8) : "",
                })
                .ForEach(item =>
                {
                    var bitQueue = item.bitQueue.Clone();
                    var actualValue = bitQueue.DequeueByte(item.direction);
                    var actualBitQueue = bitQueue.ToString("R");
                    if (!(actualValue == item.desiredValue && string.Equals(actualBitQueue, item.desiredBitQueue, StringComparison.Ordinal)))
                        Console.WriteLine(string.Format("TestBitQueue.TestDequeue.DequeueByte: 処理結果が一致しません。: bitQueue={0}, direction={1}, actualValue={2}, actualBitQueue={3}, desiredValue={4}, desiredBitQueue={5}", item.bitQueue, item.direction, actualValue, actualBitQueue, item.desiredValue, item.desiredBitQueue));
                });

            BitPatternDataSource
                .QuickDistinct(StringComparer.Ordinal)
                .Where(bitPattern => bitPattern.Length > 0)
                .SelectMany(bitPattern => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (bitPattern, direction) => new { bitPattern, direction })
                .Select(item => new
                {
                    bitQueue = new BitQueue(item.bitPattern),
                    item.direction,
                    desiredValue =
                        Convert.ToUInt16(
                            item.direction == BitPackingDirection.MsbToLsb
                            ? item.bitPattern.Length > 16 ? item.bitPattern.Substring(0, 16) : item.bitPattern
                            : item.bitPattern.Length > 16 ? new string(item.bitPattern.Substring(0, 16).Reverse().ToArray()) : new string(item.bitPattern.Reverse().ToArray()),
                            2),
                    desiredBitQueue = item.bitPattern.Length > 16 ? item.bitPattern.Substring(16) : "",
                })
                .ForEach(item =>
                {
                    var bitQueue = item.bitQueue.Clone();
                    var actualValue = bitQueue.DequeueUInt16(item.direction);
                    var actualBitQueue = bitQueue.ToString("R");
                    if (!(actualValue == item.desiredValue && string.Equals(actualBitQueue, item.desiredBitQueue, StringComparison.Ordinal)))
                        Console.WriteLine(string.Format("TestBitQueue.TestDequeue.DequeueUInt16: 処理結果が一致しません。: bitQueue={0}, direction={1}, actualValue={2}, actualBitQueue={3}, desiredValue={4}, desiredBitQueue={5}", item.bitQueue, item.direction, actualValue, actualBitQueue, item.desiredValue, item.desiredBitQueue));
                });

            BitPatternDataSource
                .QuickDistinct(StringComparer.Ordinal)
                .Where(bitPattern => bitPattern.Length > 0)
                .SelectMany(bitPattern => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (bitPattern, direction) => new { bitPattern, direction })
                .Select(item => new
                {
                    bitQueue = new BitQueue(item.bitPattern),
                    item.direction,
                    desiredValue =
                        Convert.ToUInt32(
                            item.direction == BitPackingDirection.MsbToLsb
                            ? item.bitPattern.Length > 32 ? item.bitPattern.Substring(0, 32) : item.bitPattern
                            : item.bitPattern.Length > 32 ? new string(item.bitPattern.Substring(0, 32).Reverse().ToArray()) : new string(item.bitPattern.Reverse().ToArray()),
                            2),
                    desiredBitQueue = item.bitPattern.Length > 32 ? item.bitPattern.Substring(32) : "",
                })
                .ForEach(item =>
                {
                    var bitQueue = item.bitQueue.Clone();
                    var actualValue = bitQueue.DequeueUInt32(item.direction);
                    var actualBitQueue = bitQueue.ToString("R");
                    if (!(actualValue == item.desiredValue && string.Equals(actualBitQueue, item.desiredBitQueue, StringComparison.Ordinal)))
                        Console.WriteLine(string.Format("TestBitQueue.TestDequeue.DequeueUInt32: 処理結果が一致しません。: bitQueue={0}, direction={1}, actualValue={2}, actualBitQueue={3}, desiredValue={4}, desiredBitQueue={5}", item.bitQueue, item.direction, actualValue, actualBitQueue, item.desiredValue, item.desiredBitQueue));
                });

            BitPatternDataSource
                .QuickDistinct(StringComparer.Ordinal)
                .Where(bitPattern => bitPattern.Length > 0)
                .SelectMany(bitPattern => new[] { BitPackingDirection.LsbToMsb, BitPackingDirection.MsbToLsb }, (bitPattern, direction) => new { bitPattern, direction })
                .Select(item => new
                {
                    bitQueue = new BitQueue(item.bitPattern),
                    item.direction,
                    desiredValue =
                        Convert.ToUInt64(
                            item.direction == BitPackingDirection.MsbToLsb
                            ? item.bitPattern.Length > 64 ? item.bitPattern.Substring(0, 64) : item.bitPattern
                            : item.bitPattern.Length > 64 ? new string(item.bitPattern.Substring(0, 64).Reverse().ToArray()) : new string(item.bitPattern.Reverse().ToArray()),
                            2),
                    desiredBitQueue = item.bitPattern.Length > 64 ? item.bitPattern.Substring(64) : "",
                })
                .ForEach(item =>
                {
                    var bitQueue = item.bitQueue.Clone();
                    var actualValue = bitQueue.DequeueUInt64(item.direction);
                    var actualBitQueue = bitQueue.ToString("R");
                    if (!(actualValue == item.desiredValue && string.Equals(actualBitQueue, item.desiredBitQueue, StringComparison.Ordinal)))
                        Console.WriteLine(string.Format("TestBitQueue.TestDequeue.DequeueUInt32: 処理結果が一致しません。: bitQueue={0}, direction={1}, actualValue={2}, actualBitQueue={3}, desiredValue={4}, desiredBitQueue={5}", item.bitQueue, item.direction, actualValue, actualBitQueue, item.desiredValue, item.desiredBitQueue));
                });

        }

        private static IEnumerable<int> BitCountDataSource =>
            new[] { 0, 8, 16, 32, 64, 128, 192, 256, 320, 384 }
            .SelectMany(n => new[] { n - 2, n - 1, n, n + 1, n + 2 })
            .Where(n => n >= 0);

        private static IEnumerable<string> BitPatternDataSource
        {
            get
            {
                return new[]
                {
                    new string(Enumerable.Repeat('1', 64 * 6).ToArray()),
                    new string(Enumerable.Repeat('0', 64 * 6).ToArray()),
                    string.Concat(Enumerable.Repeat("00000001", 8 * 6)),
                    string.Concat(Enumerable.Repeat("10000000", 8 * 6)),
                    string.Concat(Enumerable.Repeat("11111110", 8 * 6)),
                    string.Concat(Enumerable.Repeat("01111111", 8 * 6)),
                    string.Concat(Enumerable.Repeat("0" + new string('1', 64 - 1), 6)),
                    string.Concat(Enumerable.Repeat("1" + new string('0', 64 - 1), 6)),
                    string.Concat(Enumerable.Repeat(new string('1', 64 - 1) + "0", 6)),
                    string.Concat(Enumerable.Repeat(new string('0', 64 - 1) + "1", 6)),
                    string.Concat(Enumerable.Repeat("10110011100011110000111000110010", 12)),
                    string.Concat(Enumerable.Repeat("01001100011100001111000111001101", 12)),
                }
                .SelectMany(
                    bitArray => BitCountDataSource,
                    (bitArray, length) => bitArray.Substring(0, length.Minimum(bitArray.Length)));
            }
        }
    }
}