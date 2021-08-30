using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Utility
{
    public static class ByteArrayExtensions
    {
        private const byte _byteEscape = 0x1b;
        private const byte _byteAtMark = 0x40;
        private const byte _byteDollar = 0x24;
        private const byte _byteAmpersand = 0x26;
        private const byte _byteOpenParenthesis = 0x28;
        private const byte _byteB = 0x42;
        private const byte _byteD = 0x44;
        private const byte _byteJ = 0x4a;
        private const byte _byteI = 0x49;
        private static UInt32[] _crcTable1;
#if false
        private static UInt32[] _crcTable2;
#endif

        static ByteArrayExtensions()
        {
#if true
            _crcTable1 = new UInt32[]
            {
                0x00000000, 0x77073096, 0xee0e612c, 0x990951ba,
                0x076dc419, 0x706af48f, 0xe963a535, 0x9e6495a3,
                0x0edb8832, 0x79dcb8a4, 0xe0d5e91e, 0x97d2d988,
                0x09b64c2b, 0x7eb17cbd, 0xe7b82d07, 0x90bf1d91,
                0x1db71064, 0x6ab020f2, 0xf3b97148, 0x84be41de,
                0x1adad47d, 0x6ddde4eb, 0xf4d4b551, 0x83d385c7,
                0x136c9856, 0x646ba8c0, 0xfd62f97a, 0x8a65c9ec,
                0x14015c4f, 0x63066cd9, 0xfa0f3d63, 0x8d080df5,
                0x3b6e20c8, 0x4c69105e, 0xd56041e4, 0xa2677172,
                0x3c03e4d1, 0x4b04d447, 0xd20d85fd, 0xa50ab56b,
                0x35b5a8fa, 0x42b2986c, 0xdbbbc9d6, 0xacbcf940,
                0x32d86ce3, 0x45df5c75, 0xdcd60dcf, 0xabd13d59,
                0x26d930ac, 0x51de003a, 0xc8d75180, 0xbfd06116,
                0x21b4f4b5, 0x56b3c423, 0xcfba9599, 0xb8bda50f,
                0x2802b89e, 0x5f058808, 0xc60cd9b2, 0xb10be924,
                0x2f6f7c87, 0x58684c11, 0xc1611dab, 0xb6662d3d,
                0x76dc4190, 0x01db7106, 0x98d220bc, 0xefd5102a,
                0x71b18589, 0x06b6b51f, 0x9fbfe4a5, 0xe8b8d433,
                0x7807c9a2, 0x0f00f934, 0x9609a88e, 0xe10e9818,
                0x7f6a0dbb, 0x086d3d2d, 0x91646c97, 0xe6635c01,
                0x6b6b51f4, 0x1c6c6162, 0x856530d8, 0xf262004e,
                0x6c0695ed, 0x1b01a57b, 0x8208f4c1, 0xf50fc457,
                0x65b0d9c6, 0x12b7e950, 0x8bbeb8ea, 0xfcb9887c,
                0x62dd1ddf, 0x15da2d49, 0x8cd37cf3, 0xfbd44c65,
                0x4db26158, 0x3ab551ce, 0xa3bc0074, 0xd4bb30e2,
                0x4adfa541, 0x3dd895d7, 0xa4d1c46d, 0xd3d6f4fb,
                0x4369e96a, 0x346ed9fc, 0xad678846, 0xda60b8d0,
                0x44042d73, 0x33031de5, 0xaa0a4c5f, 0xdd0d7cc9,
                0x5005713c, 0x270241aa, 0xbe0b1010, 0xc90c2086,
                0x5768b525, 0x206f85b3, 0xb966d409, 0xce61e49f,
                0x5edef90e, 0x29d9c998, 0xb0d09822, 0xc7d7a8b4,
                0x59b33d17, 0x2eb40d81, 0xb7bd5c3b, 0xc0ba6cad,
                0xedb88320, 0x9abfb3b6, 0x03b6e20c, 0x74b1d29a,
                0xead54739, 0x9dd277af, 0x04db2615, 0x73dc1683,
                0xe3630b12, 0x94643b84, 0x0d6d6a3e, 0x7a6a5aa8,
                0xe40ecf0b, 0x9309ff9d, 0x0a00ae27, 0x7d079eb1,
                0xf00f9344, 0x8708a3d2, 0x1e01f268, 0x6906c2fe,
                0xf762575d, 0x806567cb, 0x196c3671, 0x6e6b06e7,
                0xfed41b76, 0x89d32be0, 0x10da7a5a, 0x67dd4acc,
                0xf9b9df6f, 0x8ebeeff9, 0x17b7be43, 0x60b08ed5,
                0xd6d6a3e8, 0xa1d1937e, 0x38d8c2c4, 0x4fdff252,
                0xd1bb67f1, 0xa6bc5767, 0x3fb506dd, 0x48b2364b,
                0xd80d2bda, 0xaf0a1b4c, 0x36034af6, 0x41047a60,
                0xdf60efc3, 0xa867df55, 0x316e8eef, 0x4669be79,
                0xcb61b38c, 0xbc66831a, 0x256fd2a0, 0x5268e236,
                0xcc0c7795, 0xbb0b4703, 0x220216b9, 0x5505262f,
                0xc5ba3bbe, 0xb2bd0b28, 0x2bb45a92, 0x5cb36a04,
                0xc2d7ffa7, 0xb5d0cf31, 0x2cd99e8b, 0x5bdeae1d,
                0x9b64c2b0, 0xec63f226, 0x756aa39c, 0x026d930a,
                0x9c0906a9, 0xeb0e363f, 0x72076785, 0x05005713,
                0x95bf4a82, 0xe2b87a14, 0x7bb12bae, 0x0cb61b38,
                0x92d28e9b, 0xe5d5be0d, 0x7cdcefb7, 0x0bdbdf21,
                0x86d3d2d4, 0xf1d4e242, 0x68ddb3f8, 0x1fda836e,
                0x81be16cd, 0xf6b9265b, 0x6fb077e1, 0x18b74777,
                0x88085ae6, 0xff0f6a70, 0x66063bca, 0x11010b5c,
                0x8f659eff, 0xf862ae69, 0x616bffd3, 0x166ccf45,
                0xa00ae278, 0xd70dd2ee, 0x4e048354, 0x3903b3c2,
                0xa7672661, 0xd06016f7, 0x4969474d, 0x3e6e77db,
                0xaed16a4a, 0xd9d65adc, 0x40df0b66, 0x37d83bf0,
                0xa9bcae53, 0xdebb9ec5, 0x47b2cf7f, 0x30b5ffe9,
                0xbdbdf21c, 0xcabac28a, 0x53b39330, 0x24b4a3a6,
                0xbad03605, 0xcdd70693, 0x54de5729, 0x23d967bf,
                0xb3667a2e, 0xc4614ab8, 0x5d681b02, 0x2a6f2b94,
                0xb40bbe37, 0xc30c8ea1, 0x5a05df1b, 0x2d02ef8d,
            };
#else
            _crcTable1 = new UInt32[256];
            for (var i = 0; i < 256; i++)
            {
                var c = (UInt32)i;
                for (int j = 0; j < 8; j++)
                    c = (c & 1) != 0 ? (0xEDB88320U ^ (c >> 1)) : (c >> 1);
                _crcTable1[i] = c;
            }
            for (int i = 0; i < _crcTable1.Length; i++)
            {
                if (i % 4 != 3)
                    Console.Write("0x{0:x8}, ", _crcTable1[i]);
                else
                    Console.Write("0x{0:x8},\n", _crcTable1[i]);
            }
#endif
#if false
#if true
            _crcTable2 = new UInt32[]
            {
                0x00000000, 0x04c11db7, 0x09823b6e, 0x0d4326d9,
                0x130476dc, 0x17c56b6b, 0x1a864db2, 0x1e475005,
                0x2608edb8, 0x22c9f00f, 0x2f8ad6d6, 0x2b4bcb61,
                0x350c9b64, 0x31cd86d3, 0x3c8ea00a, 0x384fbdbd,
                0x4c11db70, 0x48d0c6c7, 0x4593e01e, 0x4152fda9,
                0x5f15adac, 0x5bd4b01b, 0x569796c2, 0x52568b75,
                0x6a1936c8, 0x6ed82b7f, 0x639b0da6, 0x675a1011,
                0x791d4014, 0x7ddc5da3, 0x709f7b7a, 0x745e66cd,
                0x9823b6e0, 0x9ce2ab57, 0x91a18d8e, 0x95609039,
                0x8b27c03c, 0x8fe6dd8b, 0x82a5fb52, 0x8664e6e5,
                0xbe2b5b58, 0xbaea46ef, 0xb7a96036, 0xb3687d81,
                0xad2f2d84, 0xa9ee3033, 0xa4ad16ea, 0xa06c0b5d,
                0xd4326d90, 0xd0f37027, 0xddb056fe, 0xd9714b49,
                0xc7361b4c, 0xc3f706fb, 0xceb42022, 0xca753d95,
                0xf23a8028, 0xf6fb9d9f, 0xfbb8bb46, 0xff79a6f1,
                0xe13ef6f4, 0xe5ffeb43, 0xe8bccd9a, 0xec7dd02d,
                0x34867077, 0x30476dc0, 0x3d044b19, 0x39c556ae,
                0x278206ab, 0x23431b1c, 0x2e003dc5, 0x2ac12072,
                0x128e9dcf, 0x164f8078, 0x1b0ca6a1, 0x1fcdbb16,
                0x018aeb13, 0x054bf6a4, 0x0808d07d, 0x0cc9cdca,
                0x7897ab07, 0x7c56b6b0, 0x71159069, 0x75d48dde,
                0x6b93dddb, 0x6f52c06c, 0x6211e6b5, 0x66d0fb02,
                0x5e9f46bf, 0x5a5e5b08, 0x571d7dd1, 0x53dc6066,
                0x4d9b3063, 0x495a2dd4, 0x44190b0d, 0x40d816ba,
                0xaca5c697, 0xa864db20, 0xa527fdf9, 0xa1e6e04e,
                0xbfa1b04b, 0xbb60adfc, 0xb6238b25, 0xb2e29692,
                0x8aad2b2f, 0x8e6c3698, 0x832f1041, 0x87ee0df6,
                0x99a95df3, 0x9d684044, 0x902b669d, 0x94ea7b2a,
                0xe0b41de7, 0xe4750050, 0xe9362689, 0xedf73b3e,
                0xf3b06b3b, 0xf771768c, 0xfa325055, 0xfef34de2,
                0xc6bcf05f, 0xc27dede8, 0xcf3ecb31, 0xcbffd686,
                0xd5b88683, 0xd1799b34, 0xdc3abded, 0xd8fba05a,
                0x690ce0ee, 0x6dcdfd59, 0x608edb80, 0x644fc637,
                0x7a089632, 0x7ec98b85, 0x738aad5c, 0x774bb0eb,
                0x4f040d56, 0x4bc510e1, 0x46863638, 0x42472b8f,
                0x5c007b8a, 0x58c1663d, 0x558240e4, 0x51435d53,
                0x251d3b9e, 0x21dc2629, 0x2c9f00f0, 0x285e1d47,
                0x36194d42, 0x32d850f5, 0x3f9b762c, 0x3b5a6b9b,
                0x0315d626, 0x07d4cb91, 0x0a97ed48, 0x0e56f0ff,
                0x1011a0fa, 0x14d0bd4d, 0x19939b94, 0x1d528623,
                0xf12f560e, 0xf5ee4bb9, 0xf8ad6d60, 0xfc6c70d7,
                0xe22b20d2, 0xe6ea3d65, 0xeba91bbc, 0xef68060b,
                0xd727bbb6, 0xd3e6a601, 0xdea580d8, 0xda649d6f,
                0xc423cd6a, 0xc0e2d0dd, 0xcda1f604, 0xc960ebb3,
                0xbd3e8d7e, 0xb9ff90c9, 0xb4bcb610, 0xb07daba7,
                0xae3afba2, 0xaafbe615, 0xa7b8c0cc, 0xa379dd7b,
                0x9b3660c6, 0x9ff77d71, 0x92b45ba8, 0x9675461f,
                0x8832161a, 0x8cf30bad, 0x81b02d74, 0x857130c3,
                0x5d8a9099, 0x594b8d2e, 0x5408abf7, 0x50c9b640,
                0x4e8ee645, 0x4a4ffbf2, 0x470cdd2b, 0x43cdc09c,
                0x7b827d21, 0x7f436096, 0x7200464f, 0x76c15bf8,
                0x68860bfd, 0x6c47164a, 0x61043093, 0x65c52d24,
                0x119b4be9, 0x155a565e, 0x18197087, 0x1cd86d30,
                0x029f3d35, 0x065e2082, 0x0b1d065b, 0x0fdc1bec,
                0x3793a651, 0x3352bbe6, 0x3e119d3f, 0x3ad08088,
                0x2497d08d, 0x2056cd3a, 0x2d15ebe3, 0x29d4f654,
                0xc5a92679, 0xc1683bce, 0xcc2b1d17, 0xc8ea00a0,
                0xd6ad50a5, 0xd26c4d12, 0xdf2f6bcb, 0xdbee767c,
                0xe3a1cbc1, 0xe760d676, 0xea23f0af, 0xeee2ed18,
                0xf0a5bd1d, 0xf464a0aa, 0xf9278673, 0xfde69bc4,
                0x89b8fd09, 0x8d79e0be, 0x803ac667, 0x84fbdbd0,
                0x9abc8bd5, 0x9e7d9662, 0x933eb0bb, 0x97ffad0c,
                0xafb010b1, 0xab710d06, 0xa6322bdf, 0xa2f33668,
                0xbcb4666d, 0xb8757bda, 0xb5365d03, 0xb1f740b4,
            };

#else
            _crcTable2 = new UInt32[256];
            for (var i = 0; i < 256; i++)
            {
                var c = (UInt32)(i << 24);
                for (int j = 0; j < 8; j++)
                    c = (c << 1) ^ ((c & 0x80000000U) != 0 ? 0x04C11DB7U : 0);
                _crcTable2[i] = c;
            }
            for (int i = 0; i < _crcTable2.Length; i++)
            {
                if (i % 4 != 3)
                    Console.Write("0x{0:x8}, ", _crcTable2[i]);
                else
                    Console.Write("0x{0:x8},\n", _crcTable2[i]);
            }
#endif
#endif
        }

        public static UInt32 CalculateCrc32(this byte[] buffer)
        {
            return buffer.GetSequence().CalculateCrc32();
        }

        public static UInt32 CalculateCrc32(this byte[] buffer, int offset)
        {
            return buffer.GetSequence(offset).CalculateCrc32();
        }

        public static UInt32 CalculateCrc32(this byte[] buffer, int offset, int count)
        {
            return buffer.GetSequence(offset, count).CalculateCrc32();
        }

        public static UInt32 CalculateCrc32(this IReadOnlyArray<byte> buffer, int offset)
        {
            return buffer.GetSequence(offset).CalculateCrc32();
        }

        public static UInt32 CalculateCrc32(this IReadOnlyArray<byte> buffer, int offset, int count)
        {
            return buffer.GetSequence(offset, count).CalculateCrc32();
        }

        public static UInt32 CalculateCrc32(this IEnumerable<byte> source)
        {
#if true
            var value = 0xffffffffU;
            foreach (var data in source)
                value = _crcTable1[(value ^ data) & 0xff] ^ (value >> 8);
            return value ^ 0xffffffffU;
#else
            var value = 0xffffffffU;
            for (int i = 0; i < count; i++)
                value = (value << 8) ^ _crcTable2[((value >> 24) ^ buffer[offset + i]) & 0xff];
            return value;
#endif
        }

        public static bool IsMatchCrc(this IEnumerable<byte> source, UInt32 expectedCrc)
        {
            var actualCrc = source.CalculateCrc32();
            return actualCrc == expectedCrc;
        }

        public static bool SequenceEqual(this byte[] source, byte[] other)
        {
            if (source == null)
                throw new ArgumentNullException();
            if (other == null)
                throw new ArgumentNullException();
            if (source.Length != other.Length)
                return false;
            return source.ByteArrayEqual(0, other, 0, source.Length);
        }

        public static bool ByteArrayEqual(this byte[] byteArray1, int offset1, byte[] byteArray2, int offset2, int count)
        {
            if (byteArray1 == null)
                throw new ArgumentNullException();
            if (byteArray2 == null)
                throw new ArgumentNullException();
            if (offset1 >= byteArray1.Length)
                throw new IndexOutOfRangeException();
            if (offset1 + count > byteArray1.Length)
                throw new IndexOutOfRangeException();
            if (offset2 >= byteArray2.Length)
                throw new IndexOutOfRangeException();
            if (offset2 + count > byteArray2.Length)
                throw new IndexOutOfRangeException();

            // 第1段階: バッファの内容を8バイト単位で比較する
            var limit1OfPhaseA = offset1 + count - sizeof(UInt64);
            var index1 = offset1;
            var index2 = offset2;
            while (index1 <= limit1OfPhaseA)
            {
                var value1 = BitConverter.ToUInt64(byteArray1, index1);
                var value2 = BitConverter.ToUInt64(byteArray2, index2);
                if (value1 != value2)
                    return false;
                index1 += sizeof(UInt64);
                index2 += sizeof(UInt64);
            }

#if DEBUG
            if (offset1 + count - index1 >= sizeof(UInt64))
                throw new Exception();
#endif
            // 第2段階： バッファの内容を1バイト単位で比較する
            var limit1OfPhaseB = offset1 + count;
            // この時点で、limit1OfPhaseB - index は 8 未満のはず
            while (index1 < limit1OfPhaseB)
            {
                if (byteArray1[index1] != byteArray2[index2])
                    return false;
                ++index1;
                ++index2;
            }
#if DEBUG
            if (index1 != offset1 + count)
                throw new Exception();
            if (index2 != offset2 + count)
                throw new Exception();
#endif
            // 終端に達しても違いがなかったので、すべて一致していると判断してtrueを返す。
            return true;
        }

        public static Int16 ToInt16(this IReadOnlyArray<byte> value, int startIndex)
        {
#if DEBUG
            if (sizeof(Int16) != 2)
                throw new Exception();
#endif
            return
                BitConverter.ToInt16(new[]
                {
                    value[startIndex],
                    value[startIndex + 1],
                }, 0);
        }

        public static UInt16 ToUInt16(this IReadOnlyArray<byte> value, int startIndex)
        {
#if DEBUG
            if (sizeof(UInt16) != 2)
                throw new Exception();
#endif
            return
                BitConverter.ToUInt16(new[]
                {
                    value[startIndex],
                    value[startIndex + 1],
                }, 0);
        }

        public static Int32 ToInt32(this IReadOnlyArray<byte> value, int startIndex)
        {
#if DEBUG
            if (sizeof(Int32) != 4)
                throw new Exception();
#endif
            return
                BitConverter.ToInt32(new[]
                {
                    value[startIndex],
                    value[startIndex + 1],
                    value[startIndex + 2],
                    value[startIndex + 3],
                }, 0);
        }

        public static UInt32 ToUInt32(this IReadOnlyArray<byte> value, int startIndex)
        {
#if DEBUG
            if (sizeof(UInt32) != 4)
                throw new Exception();
#endif
            return
                BitConverter.ToUInt32(new[]
                {
                    value[startIndex],
                    value[startIndex + 1],
                    value[startIndex + 2],
                    value[startIndex + 3],
                }, 0);
        }

        public static Int64 ToInt64(this IReadOnlyArray<byte> value, int startIndex)
        {
#if DEBUG
            if (sizeof(Int64) != 8)
                throw new Exception();
#endif
            return
                BitConverter.ToInt64(new[]
                {
                    value[startIndex],
                    value[startIndex + 1],
                    value[startIndex + 2],
                    value[startIndex + 3],
                    value[startIndex + 4],
                    value[startIndex + 5],
                    value[startIndex + 6],
                    value[startIndex + 7],
                }, 0);
        }

        public static UInt64 ToUInt64(this IReadOnlyArray<byte> value, int startIndex)
        {
#if DEBUG
            if (sizeof(UInt64) != 8)
                throw new Exception();
#endif
            return
                BitConverter.ToUInt64(new[]
                {
                    value[startIndex],
                    value[startIndex + 1],
                    value[startIndex + 2],
                    value[startIndex + 3],
                    value[startIndex + 4],
                    value[startIndex + 5],
                    value[startIndex + 6],
                    value[startIndex + 7],
                }, 0);
        }

        public static float ToSingle(this IReadOnlyArray<byte> value, int startIndex)
        {
#if DEBUG
            if (sizeof(float) != 4)
                throw new Exception();
#endif
            return
                BitConverter.ToSingle(new[]
                {
                    value[startIndex],
                    value[startIndex + 1],
                    value[startIndex + 2],
                    value[startIndex + 3],
                }, 0);
        }

        public static double ToDouble(this IReadOnlyArray<byte> value, int startIndex)
        {
#if DEBUG
            if (sizeof(double) != 8)
                throw new Exception();
#endif
            return
                BitConverter.ToDouble(new[]
                {
                    value[startIndex],
                    value[startIndex + 1],
                    value[startIndex + 2],
                    value[startIndex + 3],
                    value[startIndex + 4],
                    value[startIndex + 5],
                    value[startIndex + 6],
                    value[startIndex + 7],
                }, 0);
        }

        public static string ToString(this IReadOnlyArray<byte> value)
        {
            return BitConverter.ToString(value.ToArray());
        }

        public static string ToString(this IReadOnlyArray<byte> value, int startIndex)
        {
            return BitConverter.ToString(value.Skip(startIndex).ToArray());
        }

        public static string ToString(this IReadOnlyArray<byte> value, int startIndex, int length)
        {
            return BitConverter.ToString(value.Skip(startIndex).Take(length).ToArray());
        }

        public static Encoding GuessWhichEncoding(this byte[] bytes)
        {
            return bytes.AsReadOnly().GuessWhichEncoding();
        }

        public static Encoding GuessWhichEncoding(this IReadOnlyArray<byte> bytes)
        {
            var len = bytes.Length;

            // UTF-16かどうかのチェック
            var isBinary = false;
            for (int i = 0; i < len; i++)
            {
                var b1 = bytes[i];
                if (b1 <= 0x06 || b1.IsAnyOf((byte)0x7f, (byte)0xff))
                {
                    isBinary = true;
                    if (b1 == 0x00 && i < len - 1 && bytes[i + 1] <= 0x7f)
                        return Encoding.Unicode;
                }
            }
            if (isBinary)
                return null;

            // ASCIIかどうかのチェック
            var notJapanese = true;
            for (int i = 0; i < len; i++)
            {
                var b1 = bytes[i];
                if (b1 == _byteEscape || b1 >= 0x80)
                {
                    notJapanese = false;
                    break;
                }
            }
            if (notJapanese)
                return Encoding.ASCII;

            // JISコードかどうかのチェック
            for (int i = 0; i < len - 2; i++)
            {
                var b1 = bytes[i];
                var b2 = bytes[i + 1];
                var b3 = bytes[i + 2];

                if (b1 == _byteEscape)
                {
                    if (b2 == _byteDollar && b3 == _byteAtMark)
                        return Encoding.GetEncoding(50220);//JIS_0208 1978
                    else if (b2 == _byteDollar && b3 == _byteB)
                        return Encoding.GetEncoding(50220);//JIS_0208 1983
                    else if (b2 == _byteOpenParenthesis && b3.IsAnyOf(_byteB, _byteJ))
                        return Encoding.GetEncoding(50220);//JIS_ASC
                    else if (b2 == _byteOpenParenthesis && b3 == _byteI)
                        return Encoding.GetEncoding(50220);//JIS_KANA
                    if (i < len - 3)
                    {
                        var b4 = bytes[i + 3];
                        if (b2 == _byteDollar &&
                            b3 == _byteOpenParenthesis &&
                            b4 == _byteD)
                            return Encoding.GetEncoding(50220);//JIS_0212
                        if (i < len - 5 &&
                            b2 == _byteAmpersand &&
                            b3 == _byteAtMark &&
                            b4 == _byteEscape &&
                            bytes[i + 4] == _byteDollar &&
                            bytes[i + 5] == _byteB)
                            return Encoding.GetEncoding(50220);//JIS_0208 1990
                    }
                }
            }

            // この時点で euc/shif-jis/utf-8 のいずれかしかない。
            var count_shift_jis = 0;
            var count_euc = 0;
            var count_utf8 = 0;
            for (int i = 0; i < len - 1; i++)
            {
                var b1 = bytes[i];
                var b2 = bytes[i + 1];
                if ((b1.IsBetween((byte)0x81, (byte)0x9f) || b1.IsBetween((byte)0xe0, (byte)0xfc)) &&
                    (b2.IsBetween((byte)0x40, (byte)0x7e) || b2.IsBetween((byte)0x80, (byte)0xfc)))
                {
                    //SJIS_C
                    count_shift_jis += 2;
                    i++;
                }
            }
            for (int i = 0; i < len - 1; i++)
            {
                var b1 = bytes[i];
                var b2 = bytes[i + 1];
                if (b1.IsBetween((byte)0xa1, (byte)0xfe) && b2.IsBetween((byte)0xa1, (byte)0xfe) ||
                    b1 == 0x8e && b2.IsBetween((byte)0xa1, (byte)0xdf))
                {
                    //EUC_C
                    //EUC_KANA
                    count_euc += 2;
                    i++;
                }
                else if (i < len - 2)
                {
                    if (b1 == 0x8f &&
                        b2.IsBetween((byte)0xa1, (byte)0xfe) &&
                        bytes[i + 2].IsBetween((byte)0xa1, (byte)0xfe))
                    {
                        //EUC_0212
                        count_euc += 3;
                        i += 2;
                    }
                }
            }
            for (int i = 0; i < len - 1; i++)
            {
                var b1 = bytes[i];
                var b2 = bytes[i + 1];
                if (b1.IsBetween((byte)0xc0, (byte)0xdf) &&
                    b2.IsBetween((byte)0x80, (byte)0xbf))
                {
                    //UTF8
                    count_utf8 += 2;
                    i++;
                }
                else if (i < len - 2)
                {
                    if (b1.IsBetween((byte)0xe0, (byte)0xef) &&
                        b2.IsBetween((byte)0x80, (byte)0xbf) &&
                        bytes[i + 2].IsBetween((byte)0x80, (byte)0xbf))
                    {
                        //UTF8
                        count_utf8 += 3;
                        i += 2;
                    }
                }
            }
            if (count_euc > count_shift_jis && count_euc > count_utf8)
                return Encoding.GetEncoding(51932); // euc
            else if (count_shift_jis > count_euc && count_shift_jis > count_utf8)
                return Encoding.GetEncoding(932);// shift_jis
            else if (count_utf8 > count_euc && count_utf8 > count_shift_jis)
                return Encoding.UTF8; // utf8
            else
                return null;
        }
    }
}