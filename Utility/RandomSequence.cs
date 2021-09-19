using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Utility
{
    public static class RandomSequence
    {
        private class BitArraySequence
            : IEnumerable<ReadOnlySerializedBitArray>
        {
            private int _bitCount;

            public BitArraySequence(int bitCount)
            {
                if (bitCount <= 0)
                    throw new ArgumentException("'bitCount' must not be less than or equal to zero.", "bitCount");
                _bitCount = bitCount;
            }

            public IEnumerator<ReadOnlySerializedBitArray> GetEnumerator()
            {
                return
                    new RandomBytesSequence((_bitCount + 7) >> 3)
                    .GetBitArraySequence(_bitCount)
                    .GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        static RandomSequence()
        {
            AsciiCharSequence =
                new RandomBytesSequence(5)
                .GetBitArraySequence(33)
                .SelectMany(array =>
                {
                    // 0x02.Power(33)-1 == 0x1ffffffff
                    // 0x60.Power( 5)-1 == 0x1e5ffffff
                    var ulongValue = array.ToUInt64();
                    var c1 = GetChar(ulongValue);
                    ulongValue /= 0x60;
                    var c2 = GetChar(ulongValue);
                    ulongValue /= 0x60;
                    var c3 = GetChar(ulongValue);
                    ulongValue /= 0x60;
                    var c4 = GetChar(ulongValue);
                    ulongValue /= 0x60;
                    var c5 = GetChar(ulongValue);
                    return new[] { c1, c2, c3, c4, c5 };
                });

            SByteSequence =
                new RandomBytesSequence(sizeof(sbyte))
                .Select(data => (sbyte)data[0]);

            ByteSequence =
                 new RandomBytesSequence(sizeof(byte))
                 .Select(data => data[0]);

            Int16Sequence =
                new RandomBytesSequence(sizeof(Int16))
                .Select(bytes => bytes.ToInt16LE(0));

            UInt16Sequence =
                new RandomBytesSequence(sizeof(UInt16))
                .Select(bytes => bytes.ToUInt16LE(0));

            Int32Sequence =
                new RandomBytesSequence(sizeof(Int32))
                .Select(bytes => bytes.ToInt32LE(0));

            UInt32Sequence =
                new RandomBytesSequence(sizeof(UInt32))
                .Select(bytes => bytes.ToUInt32LE(0));

            Int64Sequence =
                new RandomBytesSequence(sizeof(Int64))
                .Select(bytes => bytes.ToInt64LE(0));

            UInt64Sequence =
                new RandomBytesSequence(sizeof(UInt64))
                .Select(bytes => bytes.ToUInt64LE(0));

            SingleSequence =
                new RandomBytesSequence(sizeof(UInt32))
                .GetBitArraySequence(24)
                .Select(bits => (Single)bits.ToUInt32() / (1U << 24));

            DoubleSequence =
                new RandomBytesSequence(sizeof(UInt64))
                .GetBitArraySequence(53)
                .Select(bits => (Double)bits.ToUInt64() / (1UL << 53));
        }

        /// <summary>
        /// 与えられた長さのランダムなビット配列を要素とするシーケンスを取得します。
        /// </summary>
        /// <param name="bitCount">
        /// ビット配列の長さを示す<see cref="int"/>値です。
        /// </param>
        /// <returns>
        /// ビット配列を要素とするシーケンスを示す<see cref="IEnumerable{ReadOnlyBitArray}"/>オブジェクトです。
        /// </returns>
        /// <remarks>
        /// このメソッドで取得したシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードは長さが5ビットのランダムなビット配列を100個だけ取得します。
        /// <code>
        ///     ReadOnlyBitArray[] randomBitArrays = RandomSequence.GetBitArraySequence(5).Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<ReadOnlySerializedBitArray> GetBitArraySequence(int bitCount) => new BitArraySequence(bitCount);

        /// <summary>
        /// ランダムな表示可能な文字(\u000a, \u0020-\u007e)を要素とするシーケンスを示す<see cref="IEnumerable{Char}"/>オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな文字を100個だけ取得します。
        /// <code>
        ///     Char[] randomCharArray = RandomSequence.AsciiCharSequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<Char> AsciiCharSequence { get; }

        /// <summary>
        /// <see cref="SByte.MinValue"/>以上、<see cref="SByte.MaxValue"/>以下のランダムな値を要素とするシーケンスを示す<see cref="IEnumerable{SByte}"/>オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな<see cref="SByte"/>値を100個だけ取得します。
        /// <code>
        ///     SByte[] randomValueArray = RandomSequence.SByteSequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<SByte> SByteSequence { get; }

        /// <summary>
        /// <see cref="Byte.MinValue"/>以上、<see cref="Byte.MaxValue"/>以下のランダムな値を要素とするシーケンスを示す<see cref="IEnumerable{Byte}"/>オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな<see cref="Byte"/>値を100個だけ取得します。
        /// <code>
        ///     Byte[] randomValueArray = RandomSequence.ByteSequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<Byte> ByteSequence { get; }

        /// <summary>
        /// <see cref="Int16.MinValue"/>以上、<see cref="Int16.MaxValue"/>以下のランダムな値を要素とするシーケンスを示す<see cref="IEnumerable{Int16}"/>オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな<see cref="Int16"/>値を100個だけ取得します。
        /// <code>
        ///     Int16[] randomValueArray = RandomSequence.Int16Sequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<Int16> Int16Sequence { get; }

        /// <summary>
        /// <see cref="UInt16.MinValue"/>以上、<see cref="UInt16.MaxValue"/>以下のランダムな値を要素とするシーケンスを示す<see cref="IEnumerable{UInt16}"/>オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな<see cref="UInt16"/>値を100個だけ取得します。
        /// <code>
        ///     UInt16[] randomValueArray = RandomSequence.UInt16Sequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<UInt16> UInt16Sequence { get; }

        /// <summary>
        /// <see cref="Int32.MinValue"/>以上、<see cref="Int32.MaxValue"/>以下のランダムな値を要素とするシーケンスを示す<see cref="IEnumerable{Int32}"/>オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな<see cref="Int32"/>値を100個だけ取得します。
        /// <code>
        ///     Int32[] randomValueArray = RandomSequence.Int32Sequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<Int32> Int32Sequence { get; }

        /// <summary>
        /// <see cref="UInt32.MinValue"/>以上、<see cref="UInt32.MaxValue"/>以下のランダムな値を要素とするシーケンスを示す<see cref="IEnumerable{UInt32}"/>オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな<see cref="UInt32"/>値を100個だけ取得します。
        /// <code>
        ///     UInt32[] randomValueArray = RandomSequence.UInt32Sequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<UInt32> UInt32Sequence { get; }

        /// <summary>
        /// <see cref="Int64.MinValue"/>以上、<see cref="Int64.MaxValue"/>以下のランダムな値を要素とするシーケンスを示す<see cref="IEnumerable{Int64}"/>オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな<see cref="Int64"/>値を100個だけ取得します。
        /// <code>
        ///     Int64[] randomValueArray = RandomSequence.Int64Sequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<Int64> Int64Sequence { get; }

        /// <summary>
        /// <see cref="UInt64.MinValue"/>以上、<see cref="UInt64.MaxValue"/>以下のランダムな値を要素とするシーケンスを示す<see cref="IEnumerable{UInt64}"/>オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな<see cref="UInt64"/>値を100個だけ取得します。
        /// <code>
        ///     UInt64[] randomValueArray = RandomSequence.UInt64Sequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<UInt64> UInt64Sequence { get; }

        /// <summary>
        /// 0.0 以上、1.0 未満のランダムな値を要素とするシーケンスを示す<see cref="IEnumerable{Single}"/>オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな<see cref="Single"/>値を100個だけ取得します。
        /// <code>
        ///     Single[] randomValueArray = RandomSequence.SingleSequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<Single> SingleSequence { get; }

        /// <summary>
        /// 0.0 以上、1.0 未満のランダムな値を要素とするシーケンスを示す<see cref="IEnumerable{Double}"/>オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな<see cref="Double"/>値を100個だけ取得します。
        /// <code>
        ///     Double[] randomValueArray = RandomSequence.DoubleSequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<Double> DoubleSequence { get; }

        private static char GetChar(ulong x)
        {
            var c = x % 0x60 + 0x20;
            return c == 0x7f ? '\n' : (char)c;
        }
    }
}
