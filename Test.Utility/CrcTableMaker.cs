using System;
using System.Collections.Generic;
using System.Linq;

namespace Test.Utility
{
    static class CrcTableMaker
    {
        public static IEnumerable<string> GetCrc32Table()
        {
            return
                GetCrcTable(
                    "_crcTableOfCommonCrc32",
                    index =>
                    {
                        const UInt32 CRC32_POLY = 0xedb88320U;
                        var data = (UInt32)index;
                        for (var bitIndex = 0; bitIndex < 8; bitIndex++)
                            data = (data & 1) != 0 ? (CRC32_POLY ^ (data >> 1)) : (data >> 1);
                        return data;
                    });
        }

        public static IEnumerable<string> GetRadix64Table()
        {
            return
                GetCrcTable(
                    "_crcTableOfCrc24ForRadix64",
                    index =>
                    {
                        const UInt32 CRC24_POLY = 0x01864cfb;
                        var data = (UInt32)index << 16;
                        for (var count = 7; count >= 0; --count)
                            data = (data & (0x800000)) != 0 ? CRC24_POLY ^ (data << 1) : data << 1;
                        return data;
                    });
        }

        private static IEnumerable<string> GetCrcTable(string name, Func<int, UInt32> calculateTable)
        {
            var crc32Table = new UInt32[256];
            for (var wordIndex = 0; wordIndex < 256; wordIndex++)
                crc32Table[wordIndex] = calculateTable(wordIndex);
            return
                new[]
                {
                    string.Format("{0} = new UInt32[]", name),
                    "{",
                }
                .Concat(
                    Enumerable.Range(0, 256 / 4)
                    .Select(n =>
                        string.Format(
                            "    0x{0:x8}, 0x{1:x8}, 0x{2:x8}, 0x{3:x8},",
                            crc32Table[n * 4 + 0],
                            crc32Table[n * 4 + 1],
                            crc32Table[n * 4 + 2],
                            crc32Table[n * 4 + 3])))
                .Concat(new[]
                {
                    "};"
                });
        }
    }
}
