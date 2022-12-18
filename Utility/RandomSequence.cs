using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Utility
{
    public static class RandomSequence
    {
        /// <summary>
        /// 与えられた長さのランダムなビット配列を要素とするシーケンスを取得します。
        /// </summary>
        /// <param name="bitCount">
        /// ビット配列の長さを示す <see cref="Int32"/> 値です。
        /// </param>
        /// <returns>
        /// ビット配列を要素とするシーケンスを示す <see cref="IEnumerable{ReadOnlyBitArray}">IEnumerable&lt;<see cref="ReadOnlyBitArray"/>&gt;</see> オブジェクトです。
        /// </returns>
        /// <remarks>
        /// このメソッドで取得したシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードは長さが5ビットのランダムなビット配列を100個だけ取得します。
        /// <code>
        ///    ReadOnlyBitArray[] randomBitArrays = RandomSequence.GetBitArraySequence(5).Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<TinyBitArray> GetBitArraySequence(Int32 bitCount)
        {
            if (bitCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(bitCount));

            using var generator = RandomNumberGenerator.Create();
            var bitQueue = new BitQueue();
            var buffer = new Byte[(bitCount + 7) / 8];
            while (true)
            {
                generator.GetBytes(buffer);
                foreach (var data in buffer)
                    bitQueue.Enqueue(data);
                while (bitQueue.Count >= bitCount)
                    yield return bitQueue.DequeueBitArray(bitCount);
            }
        }

        /// <summary>
        /// <see cref="Boolean"/> 値 ( true または false ) のランダムな値を要素とするシーケンスを示す <see cref="IEnumerable{Boolean}">IEnumerable&lt;<see cref="Boolean"/>&gt;</see> オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな <see cref="SByte"/> 値を100個だけ取得します。
        /// <code>
        ///    SByte[] randomValueArray = RandomSequence.SByteSequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<Boolean> GetBooleanSequence()
        {
            using var generator = RandomNumberGenerator.Create();
            var bitQueue = new BitQueue();
            while (true)
            {
                var value = generator.GetRandomUInt32Value();
                yield return (value & (1U << 0)) != 0;
                yield return (value & (1U << 1)) != 0;
                yield return (value & (1U << 2)) != 0;
                yield return (value & (1U << 3)) != 0;
                yield return (value & (1U << 4)) != 0;
                yield return (value & (1U << 5)) != 0;
                yield return (value & (1U << 6)) != 0;
                yield return (value & (1U << 7)) != 0;
                yield return (value & (1U << 8)) != 0;
                yield return (value & (1U << 9)) != 0;
                yield return (value & (1U << 10)) != 0;
                yield return (value & (1U << 11)) != 0;
                yield return (value & (1U << 12)) != 0;
                yield return (value & (1U << 13)) != 0;
                yield return (value & (1U << 14)) != 0;
                yield return (value & (1U << 15)) != 0;
                yield return (value & (1U << 16)) != 0;
                yield return (value & (1U << 17)) != 0;
                yield return (value & (1U << 18)) != 0;
                yield return (value & (1U << 19)) != 0;
                yield return (value & (1U << 20)) != 0;
                yield return (value & (1U << 21)) != 0;
                yield return (value & (1U << 22)) != 0;
                yield return (value & (1U << 23)) != 0;
                yield return (value & (1U << 24)) != 0;
                yield return (value & (1U << 25)) != 0;
                yield return (value & (1U << 26)) != 0;
                yield return (value & (1U << 27)) != 0;
                yield return (value & (1U << 28)) != 0;
                yield return (value & (1U << 29)) != 0;
                yield return (value & (1U << 30)) != 0;
                yield return (value & (1U << 31)) != 0;
            }
        }

        /// <summary>
        /// ランダムな表示可能な文字(\u000a, \u0020-\u007e)を要素とするシーケンスを示す <see cref="IEnumerable{Char}">IEnumerable&lt;<see cref="Char"/>&gt;</see> オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな文字を100個だけ取得します。
        /// <code>
        ///    Char[] randomCharArray = RandomSequence.AsciiCharSequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<Char> GetAsciiCharSequence()
        {
            // 0x02.Power(33)-1 == 0x1ffffffff
            // 0x60.Power( 5)-1 == 0x1e5ffffff

            const int BIT_SET_COUNT = 33;

            using var generator = RandomNumberGenerator.Create();
            var bitQueue = new BitQueue();
            while (true)
            {
                bitQueue.Enqueue(generator.GetRandomUInt32Value());
                while (bitQueue.Count >= BIT_SET_COUNT)
                {
                    var value = bitQueue.DequeueBitArray(BIT_SET_COUNT).ToUInt64();
                    yield return GetChar(value);
                    value /= 0x60;
                    yield return GetChar(value);
                    value /= 0x60;
                    yield return GetChar(value);
                    value /= 0x60;
                    yield return GetChar(value);
                    value /= 0x60;
                    yield return GetChar(value);
                }
            }
        }

        /// <summary>
        /// <see cref="SByte.MinValue"/ >以上、 <see cref="SByte.MaxValue"/> 以下のランダムな値を要素とするシーケンスを示す <see cref="IEnumerable{SByte}">IEnumerable&lt;<see cref="SByte"/>&gt;</see> オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな <see cref="SByte"/> 値を100個だけ取得します。
        /// <code>
        ///    SByte[] randomValueArray = RandomSequence.SByteSequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<SByte> GetSByteSequence()
        {
            using var generator = RandomNumberGenerator.Create();
            while (true)
                yield return (SByte)generator.GetRandomByteValue();
        }

        /// <summary>
        /// <see cref="Byte.MinValue"/> 以上、 <see cref="Byte.MaxValue"/> 以下のランダムな値を要素とするシーケンスを示す <see cref="IEnumerable{Byte}">IEnumerable&lt;<see cref="Byte"/>&gt;</see> オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな <see cref="Byte"/> 値を100個だけ取得します。
        /// <code>
        ///    Byte[] randomValueArray = RandomSequence.ByteSequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<Byte> GetByteSequence()
        {
            using var generator = RandomNumberGenerator.Create();
            while (true)
                yield return generator.GetRandomByteValue();
        }

        /// <summary>
        /// <see cref="Int16.MinValue"/> 以上、 <see cref="Int16.MaxValue"/> 以下のランダムな値を要素とするシーケンスを示す <see cref="IEnumerable{Int16}">IEnumerable&lt;<see cref="Int16"/>&gt;</see> オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな <see cref="Int16"/> 値を100個だけ取得します。
        /// <code>
        ///    Int16[] randomValueArray = RandomSequence.Int16Sequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<Int16> GetInt16Sequence()
        {
            using var generator = RandomNumberGenerator.Create();
            while (true)
                yield return (Int16)generator.GetRandomUInt16Value();
        }

        /// <summary>
        /// <see cref="UInt16.MinValue"/> 以上、 <see cref="UInt16.MaxValue"/> 以下のランダムな値を要素とするシーケンスを示す <see cref="IEnumerable{UInt16}">IEnumerable&lt;<see cref="UInt16"/>&gt;</see> オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな <see cref="UInt16"/> 値を100個だけ取得します。
        /// <code>
        ///    UInt16[] randomValueArray = RandomSequence.UInt16Sequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<UInt16> GetUInt16Sequence()
        {
            using var generator = RandomNumberGenerator.Create();
            while (true)
                yield return generator.GetRandomUInt16Value();
        }

        /// <summary>
        /// <see cref="Int32.MinValue"/> 以上、 <see cref="Int32.MaxValue"/> 以下のランダムな値を要素とするシーケンスを示す <see cref="IEnumerable{Int32}">IEnumerable&lt;<see cref="Int32"/>&gt;</see> オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな <see cref="Int32"/> 値を100個だけ取得します。
        /// <code>
        ///    Int32[] randomValueArray = RandomSequence.Int32Sequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<Int32> GetInt32Sequence()
        {
            using var generator = RandomNumberGenerator.Create();
            while (true)
                yield return (Int32)generator.GetRandomUInt32Value();
        }

        /// <summary>
        /// <see cref="UInt32.MinValue"/> 以上、 <see cref="UInt32.MaxValue"/> 以下のランダムな値を要素とするシーケンスを示す <see cref="IEnumerable{UInt32}">IEnumerable&lt;<see cref="UInt32"/>&gt;</see> オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな <see cref="UInt32"/> 値を100個だけ取得します。
        /// <code>
        ///    UInt32[] randomValueArray = RandomSequence.UInt32Sequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<UInt32> GetUInt32Sequence()
        {
            using var generator = RandomNumberGenerator.Create();
            while (true)
                yield return generator.GetRandomUInt32Value();
        }

        /// <summary>
        /// <see cref="Int64.MinValue"/> 以上、 <see cref="Int64.MaxValue"/> 以下のランダムな値を要素とするシーケンスを示す <see cref="IEnumerable{Int64}">IEnumerable&lt;<see cref="Int64"/>&gt;</see> オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな <see cref="Int64"/> 値を100個だけ取得します。
        /// <code>
        ///    Int64[] randomValueArray = RandomSequence.Int64Sequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<Int64> GetInt64Sequence()
        {
            using var generator = RandomNumberGenerator.Create();
            while (true)
                yield return (Int64)generator.GetRandomUInt64Value();
        }

        /// <summary>
        /// <see cref="UInt64.MinValue"/> 以上、 <see cref="UInt64.MaxValue"/> 以下のランダムな値を要素とするシーケンスを示す <see cref="IEnumerable{UInt64}">IEnumerable&lt;<see cref="UInt64"/>&gt;</see> オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな <see cref="UInt64"/> 値を100個だけ取得します。
        /// <code>
        ///    UInt64[] randomValueArray = RandomSequence.UInt64Sequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<UInt64> GetUInt64Sequence()
        {
            using var generator = RandomNumberGenerator.Create();
            while (true)
                yield return generator.GetRandomUInt64Value();
        }

        /// <summary>
        /// 0.0 以上、1.0 未満のランダムな値を要素とするシーケンスを示す <see cref="IEnumerable{Single}">IEnumerable&lt;<see cref="Single"/>&gt;</see> オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな <see cref="Single"/> 値を100個だけ取得します。
        /// <code>
        ///    Single[] randomValueArray = RandomSequence.SingleSequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<Single> GetSingleSequence()
        {
            const int BIT_SET_COUNT = 24;

            using var generator = RandomNumberGenerator.Create();
            var bitQueue = new BitQueue();
            while (true)
            {
                bitQueue.Enqueue(generator.GetRandomUInt32Value());
                while (bitQueue.Count >= BIT_SET_COUNT)
                    yield return (Single)bitQueue.DequeueBitArray(BIT_SET_COUNT).ToUInt32() / (1U << BIT_SET_COUNT);
            }
        }

        /// <summary>
        /// 0.0 以上、1.0 未満のランダムな値を要素とするシーケンスを示す <see cref="IEnumerable{Double}">IEnumerable&lt;<see cref="Double"/>&gt;</see> オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな <see cref="Double"/> 値を100個だけ取得します。
        /// <code>
        ///    Double[] randomValueArray = RandomSequence.DoubleSequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<Double> GetDoubleSequence()
        {
            const int BIT_SET_COUNT = 53;

            using var generator = RandomNumberGenerator.Create();
            var bitQueue = new BitQueue();
            while (true)
            {
                bitQueue.Enqueue(generator.GetRandomUInt32Value());
                while (bitQueue.Count >= BIT_SET_COUNT)
                    yield return (Double)bitQueue.DequeueBitArray(BIT_SET_COUNT).ToUInt64() / (1U << BIT_SET_COUNT);
            }
        }

        /// <summary>
        /// 0.0 以上、1.0 未満のランダムな値を要素とするシーケンスを示す <see cref="IEnumerable{Decimal}">IEnumerable&lt;<see cref="Decimal"/>&gt;</see> オブジェクトです。
        /// </summary>
        /// <remarks>
        /// このシーケンスは終了せず永遠に続きます。
        /// 必要な長さの要素が取得出来たらシーケンスの列挙を打ち切ってください。(例: Take 拡張メソッドを使用する)
        /// </remarks>
        /// <example>
        /// 以下のコードはランダムな <see cref="Decimal"/> 値を100個だけ取得します。
        /// <code>
        ///    Decimal[] randomValueArray = RandomSequence.DecimalSequence.Take(100).ToArray();
        /// </code>
        /// </example>
        public static IEnumerable<Decimal> GetDecimalSequence()
        {
            using var generator = RandomNumberGenerator.Create();
            while (true)
            {
                var value = (Decimal)generator.GetRandomUInt32Value();
                value /= 1UL << 32;
                value += (Decimal)generator.GetRandomUInt32Value();
                value /= 1UL << 32;
                value += (Decimal)generator.GetRandomUInt32Value();
                value /= 1UL << 32;
                yield return value;
            }
        }

        private static char GetChar(UInt64 x)
        {
            var c = x % 0x60 + 0x20;
            return c == 0x7f ? '\n' : (char)c;
        }

        private static RandomNumberGenerator CreateRandomValueGenerator()
        {
            return RandomNumberGenerator.Create();
        }

        private static Byte GetRandomByteValue(this RandomNumberGenerator generator)
        {
            Span<Byte> buffer = stackalloc Byte[sizeof(UInt32)];
            generator.GetBytes(buffer);
            return buffer[0];
        }

        private static UInt16 GetRandomUInt16Value(this RandomNumberGenerator generator)
        {
            Span<Byte> buffer = stackalloc Byte[sizeof(UInt16)];
            generator.GetBytes(buffer);
            return buffer.ToUInt16LE();
        }

        private static UInt32 GetRandomUInt32Value(this RandomNumberGenerator generator)
        {
            Span<Byte> buffer = stackalloc Byte[sizeof(UInt32)];
            generator.GetBytes(buffer);
            return buffer.ToUInt32LE();
        }

        private static UInt64 GetRandomUInt64Value(this RandomNumberGenerator generator)
        {
            Span<Byte> buffer = stackalloc Byte[sizeof(UInt64)];
            generator.GetBytes(buffer);
            return buffer.ToUInt64LE();
        }
    }
}
