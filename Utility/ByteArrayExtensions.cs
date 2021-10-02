using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utility
{
    public static class ByteArrayExtensions
    {
        private class CommonCrc32
            : CrcCalculationMethod<UInt32>
        {
            protected override UInt32 InitialValue => 0xffffffffU;
            protected override UInt32 Update(UInt32 crc, byte data) => _crcTableOfCommonCrc32[(crc ^ data) & 0xff] ^ (crc >> 8);
            protected override UInt32 Finalize(UInt32 crc) => ~crc;
        }

        private class Crc24ForRadix64
            : CrcCalculationMethod<UInt32>
        {
            protected override UInt32 InitialValue => 0x00b704ceU;
            protected override UInt32 Update(UInt32 crc, byte data) => _crcTableOfCrc24ForRadix64[((crc >> 16) ^ data) & 0xff] ^ (crc << 8);
            protected override UInt32 Finalize(UInt32 crc) => crc & 0xffffffU;
        }

        private class BitArraySequenceFromByteSequence
            : IEnumerable<TinyBitArray>
        {
            private class Enumerator
                : IEnumerator<TinyBitArray>
            {
                private bool _isDisposed;
                private IEnumerable<byte> _source;
                private int _bitCount;
                private BitPackingDirection _packingDirection;
                private IEnumerator<byte> _sourceEnumerator;
                private BitQueue _bitQueue;
                private TinyBitArray _value;
                private bool _isEndOfSourceSequence;
                private bool _isEndOfSequence;

                public Enumerator(IEnumerable<byte> source, int bitCount, BitPackingDirection packingDirection)
                {
                    _isDisposed = false;
                    _source = source;
                    _bitCount = bitCount;
                    _packingDirection = packingDirection;
                    _sourceEnumerator = _source.GetEnumerator();
                    _bitQueue = new BitQueue();
                    _value = null;
                    _isEndOfSourceSequence = false;
                    _isEndOfSequence = false;
                }

                public TinyBitArray Current
                {
                    get
                    {
                        if (_isDisposed)
                            throw new ObjectDisposedException(GetType().FullName);
                        if (_value == null)
                            throw new InvalidOperationException();
                        if (_isEndOfSequence)
                            throw new InvalidOperationException();
                        return _value;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _value = null;
                    if (_isEndOfSequence)
                        return false;
                    while (_bitQueue.Count < _bitCount)
                    {
                        if (_isEndOfSourceSequence || _sourceEnumerator.MoveNext() == false)
                        {
                            _isEndOfSourceSequence = true;
                            break;
                        }
                        _bitQueue.Enqueue(_sourceEnumerator.Current, packingDirection: _packingDirection);
                    }
                    if (_bitQueue.Count <= 0)
                    {
                        _isEndOfSequence = true;
                        return false;
                    }
                    else
                    {
                        _value = _bitQueue.DequeueBitArray(_bitCount.Minimum(_bitQueue.Count));
                        return true;
                    }
                }

                public void Reset()
                {
                    if (_sourceEnumerator != null)
                        _sourceEnumerator.Dispose();
                    _sourceEnumerator = _source.GetEnumerator();
                    _bitQueue = new BitQueue();
                    _value = null;
                    _isEndOfSourceSequence = false;
                    _isEndOfSequence = false;
                }

                protected virtual void Dispose(bool disposing)
                {
                    if (!_isDisposed)
                    {
                        if (disposing)
                        {
                            if (_sourceEnumerator != null)
                            {
                                _sourceEnumerator.Dispose();
                                _sourceEnumerator = null;
                            }
                        }
                        _isDisposed = true;
                    }
                }

                public void Dispose()
                {
                    Dispose(disposing: true);
                    GC.SuppressFinalize(this);
                }
            }

            private IEnumerable<byte> _source;
            private int _bitCount;
            private BitPackingDirection _packingDirection;

            public BitArraySequenceFromByteSequence(IEnumerable<byte> source, int bitCount, BitPackingDirection packingDirection)
            {
                if (source == null)
                    throw new ArgumentNullException();
                if (bitCount < 1)
                    throw new ArgumentException();
                _source = source;
                _bitCount = bitCount;
                _packingDirection = packingDirection;
            }

            public IEnumerator<TinyBitArray> GetEnumerator()
            {
                return new Enumerator(_source, _bitCount, _packingDirection);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class ByteSequenceFromBitArraySequence
            : IEnumerable<Byte>
        {
            private class Enumerator
                : IEnumerator<Byte>
            {
                private bool _isDisposed;
                private IEnumerable<TinyBitArray> _source;
                private BitPackingDirection _packingDirection;
                private IEnumerator<TinyBitArray> _sourceEnumerator;
                private BitQueue _bitQueue;
                private byte? _value;
                private bool _isEndOfSourceSequence;
                private bool _isEndOfSequence;

                public Enumerator(IEnumerable<TinyBitArray> source, BitPackingDirection packingDirection)
                {
                    _isDisposed = false;
                    _source = source;
                    _packingDirection = packingDirection;
                    _sourceEnumerator = _source.GetEnumerator();
                    _bitQueue = new BitQueue();
                    _value = null;
                    _isEndOfSourceSequence = false;
                    _isEndOfSequence = false;
                }

                public Byte Current
                {
                    get
                    {
                        if (_isDisposed)
                            throw new ObjectDisposedException(GetType().FullName);
                        if (_value == null)
                            throw new InvalidOperationException();
                        if (_isEndOfSequence)
                            throw new InvalidOperationException();
                        return _value.Value;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _value = null;
                    if (_isEndOfSequence)
                        return false;
                    while (_bitQueue.Count < 8)
                    {
                        if (_isEndOfSourceSequence || _sourceEnumerator.MoveNext() == false)
                        {
                            _isEndOfSourceSequence = true;
                            break;
                        }
                        _bitQueue.Enqueue(_sourceEnumerator.Current);
                    }
                    if (_bitQueue.Count <= 0)
                    {
                        _isEndOfSequence = true;
                        return false;
                    }
                    else
                    {
                        _value = _bitQueue.DequeueByte(_packingDirection);
                        return true;
                    }
                }

                public void Reset()
                {
                    if (_sourceEnumerator != null)
                        _sourceEnumerator.Dispose();
                    _sourceEnumerator = _source.GetEnumerator();
                    _bitQueue = new BitQueue();
                    _value = null;
                    _isEndOfSourceSequence = false;
                    _isEndOfSequence = false;
                }

                protected virtual void Dispose(bool disposing)
                {
                    if (!_isDisposed)
                    {
                        if (disposing)
                        {
                            if (_sourceEnumerator != null)
                            {
                                _sourceEnumerator.Dispose();
                                _sourceEnumerator = null;
                            }
                        }
                        _isDisposed = true;
                    }
                }

                public void Dispose()
                {
                    Dispose(disposing: true);
                    GC.SuppressFinalize(this);
                }
            }

            private IEnumerable<TinyBitArray> _source;
            private BitPackingDirection _packingDirection;

            public ByteSequenceFromBitArraySequence(IEnumerable<TinyBitArray> source, BitPackingDirection packingDirection)
            {
                if (source == null)
                    throw new ArgumentNullException();
                _source = source;
                _packingDirection = packingDirection;
            }

            public IEnumerator<Byte> GetEnumerator()
            {
                return new Enumerator(_source, _packingDirection);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private const byte _byteEscape = 0x1b;
        private const byte _byteAtMark = 0x40;
        private const byte _byteDollar = 0x24;
        private const byte _byteAmpersand = 0x26;
        private const byte _byteOpenParenthesis = 0x28;
        private const byte _byteB = 0x42;
        private const byte _byteD = 0x44;
        private const byte _byteJ = 0x4a;
        private const byte _byteI = 0x49;
        private static UInt32[] _crcTableOfCommonCrc32;
        private static UInt32[] _crcTableOfCrc24ForRadix64;
        private static CrcCalculationMethod<UInt32> _commonCrc32;
        private static CrcCalculationMethod<UInt32> _crc24ForRadix64;

        static ByteArrayExtensions()
        {
            _crcTableOfCommonCrc32 = new UInt32[]
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

            _crcTableOfCrc24ForRadix64 = new UInt32[]
            {
                0x00000000, 0x00864cfb, 0x008ad50d, 0x000c99f6,
                0x0093e6e1, 0x0015aa1a, 0x001933ec, 0x009f7f17,
                0x00a18139, 0x0027cdc2, 0x002b5434, 0x00ad18cf,
                0x003267d8, 0x00b42b23, 0x00b8b2d5, 0x003efe2e,
                0x00c54e89, 0x00430272, 0x004f9b84, 0x00c9d77f,
                0x0056a868, 0x00d0e493, 0x00dc7d65, 0x005a319e,
                0x0064cfb0, 0x00e2834b, 0x00ee1abd, 0x00685646,
                0x00f72951, 0x007165aa, 0x007dfc5c, 0x00fbb0a7,
                0x000cd1e9, 0x008a9d12, 0x008604e4, 0x0000481f,
                0x009f3708, 0x00197bf3, 0x0015e205, 0x0093aefe,
                0x00ad50d0, 0x002b1c2b, 0x002785dd, 0x00a1c926,
                0x003eb631, 0x00b8faca, 0x00b4633c, 0x00322fc7,
                0x00c99f60, 0x004fd39b, 0x00434a6d, 0x00c50696,
                0x005a7981, 0x00dc357a, 0x00d0ac8c, 0x0056e077,
                0x00681e59, 0x00ee52a2, 0x00e2cb54, 0x006487af,
                0x00fbf8b8, 0x007db443, 0x00712db5, 0x00f7614e,
                0x0019a3d2, 0x009fef29, 0x009376df, 0x00153a24,
                0x008a4533, 0x000c09c8, 0x0000903e, 0x0086dcc5,
                0x00b822eb, 0x003e6e10, 0x0032f7e6, 0x00b4bb1d,
                0x002bc40a, 0x00ad88f1, 0x00a11107, 0x00275dfc,
                0x00dced5b, 0x005aa1a0, 0x00563856, 0x00d074ad,
                0x004f0bba, 0x00c94741, 0x00c5deb7, 0x0043924c,
                0x007d6c62, 0x00fb2099, 0x00f7b96f, 0x0071f594,
                0x00ee8a83, 0x0068c678, 0x00645f8e, 0x00e21375,
                0x0015723b, 0x00933ec0, 0x009fa736, 0x0019ebcd,
                0x008694da, 0x0000d821, 0x000c41d7, 0x008a0d2c,
                0x00b4f302, 0x0032bff9, 0x003e260f, 0x00b86af4,
                0x002715e3, 0x00a15918, 0x00adc0ee, 0x002b8c15,
                0x00d03cb2, 0x00567049, 0x005ae9bf, 0x00dca544,
                0x0043da53, 0x00c596a8, 0x00c90f5e, 0x004f43a5,
                0x0071bd8b, 0x00f7f170, 0x00fb6886, 0x007d247d,
                0x00e25b6a, 0x00641791, 0x00688e67, 0x00eec29c,
                0x003347a4, 0x00b50b5f, 0x00b992a9, 0x003fde52,
                0x00a0a145, 0x0026edbe, 0x002a7448, 0x00ac38b3,
                0x0092c69d, 0x00148a66, 0x00181390, 0x009e5f6b,
                0x0001207c, 0x00876c87, 0x008bf571, 0x000db98a,
                0x00f6092d, 0x007045d6, 0x007cdc20, 0x00fa90db,
                0x0065efcc, 0x00e3a337, 0x00ef3ac1, 0x0069763a,
                0x00578814, 0x00d1c4ef, 0x00dd5d19, 0x005b11e2,
                0x00c46ef5, 0x0042220e, 0x004ebbf8, 0x00c8f703,
                0x003f964d, 0x00b9dab6, 0x00b54340, 0x00330fbb,
                0x00ac70ac, 0x002a3c57, 0x0026a5a1, 0x00a0e95a,
                0x009e1774, 0x00185b8f, 0x0014c279, 0x00928e82,
                0x000df195, 0x008bbd6e, 0x00872498, 0x00016863,
                0x00fad8c4, 0x007c943f, 0x00700dc9, 0x00f64132,
                0x00693e25, 0x00ef72de, 0x00e3eb28, 0x0065a7d3,
                0x005b59fd, 0x00dd1506, 0x00d18cf0, 0x0057c00b,
                0x00c8bf1c, 0x004ef3e7, 0x00426a11, 0x00c426ea,
                0x002ae476, 0x00aca88d, 0x00a0317b, 0x00267d80,
                0x00b90297, 0x003f4e6c, 0x0033d79a, 0x00b59b61,
                0x008b654f, 0x000d29b4, 0x0001b042, 0x0087fcb9,
                0x001883ae, 0x009ecf55, 0x009256a3, 0x00141a58,
                0x00efaaff, 0x0069e604, 0x00657ff2, 0x00e33309,
                0x007c4c1e, 0x00fa00e5, 0x00f69913, 0x0070d5e8,
                0x004e2bc6, 0x00c8673d, 0x00c4fecb, 0x0042b230,
                0x00ddcd27, 0x005b81dc, 0x0057182a, 0x00d154d1,
                0x0026359f, 0x00a07964, 0x00ace092, 0x002aac69,
                0x00b5d37e, 0x00339f85, 0x003f0673, 0x00b94a88,
                0x0087b4a6, 0x0001f85d, 0x000d61ab, 0x008b2d50,
                0x00145247, 0x00921ebc, 0x009e874a, 0x0018cbb1,
                0x00e37b16, 0x006537ed, 0x0069ae1b, 0x00efe2e0,
                0x00709df7, 0x00f6d10c, 0x00fa48fa, 0x007c0401,
                0x0042fa2f, 0x00c4b6d4, 0x00c82f22, 0x004e63d9,
                0x00d11cce, 0x00575035, 0x005bc9c3, 0x00dd8538,
            };

            _commonCrc32 = new CommonCrc32();
            _crc24ForRadix64 = new Crc24ForRadix64();
        }

        public static UInt32 CalculateCrc32(this IEnumerable<byte> source) => _commonCrc32.Calculate(source);
        public static UInt32 CalculateCrc24(this IEnumerable<byte> source) => _crc24ForRadix64.Calculate(source);
        public static UInt32 CalculateCrc32(this IEnumerable<byte> source, out ulong count) => _commonCrc32.Calculate(source, out count);
        public static UInt32 CalculateCrc24(this IEnumerable<byte> source, out ulong count) => _crc24ForRadix64.Calculate(source, out count);
        public static IEnumerable<byte> GetSequenceWithCrc32(this IEnumerable<byte> source, ValueHolder<UInt32> result) => _commonCrc32.GetSequenceWithCrc(source, result);
        public static IEnumerable<byte> GetSequenceWithCrc24(this IEnumerable<byte> source, ValueHolder<UInt32> result) => _crc24ForRadix64.GetSequenceWithCrc(source, result);

        public static IEnumerable<char> GetBase64EncodedSequence(this IEnumerable<byte> source, char char62 = '+', char char63 = '/')
        {
            if (char62.IsBetween('0', '9') || char62.IsBetween('A', 'Z') || char62.IsBetween('a', 'z') || char62 == '=' || char62 <= '\u0020' || char62 >= '\u007f')
                throw new ArgumentException();
            if (char63.IsBetween('0', '9') || char63.IsBetween('A', 'Z') || char63.IsBetween('a', 'z') || char63 == '=' || char63 <= '\u0020' || char63 >= '\u007f')
                throw new ArgumentException();
            if (char62 == char63)
                throw new ArgumentException();
            return InternalGetBase64EncodedSequence(source, char62, char63);
        }

        public static IEnumerable<byte> GetBase64DecodedSequence(this IEnumerable<char> source, bool ignoreSpace = false, bool ignoreInvalidCharacter = false, char char62 = '+', char char63 = '/')
        {
            if (char62.IsBetween('0', '9') || char62.IsBetween('A', 'Z') || char62.IsBetween('a', 'z') || char62 == '=' || char62 <= '\u0020' || char62 >= '\u007f')
                throw new ArgumentException();
            if (char63.IsBetween('0', '9') || char63.IsBetween('A', 'Z') || char63.IsBetween('a', 'z') || char63 == '=' || char63 <= '\u0020' || char63 >= '\u007f')
                throw new ArgumentException();
            if (char62 == char63)
                throw new ArgumentException();
            return InternalGetBase64DecodedSequence(source, ignoreSpace, ignoreInvalidCharacter, char62, char63);
        }

        public static string EncodeBase64(this IEnumerable<byte> source, Base64EncodingType encodingType = Base64EncodingType.Default)
        {
            switch (encodingType)
            {
                case Base64EncodingType.Rrc4648Encoding: // Default
                case Base64EncodingType.Rrc2045Encoding: // for MIME
                    return
                        string.Join(
                            "\r\n",
                            source.GetBase64EncodedSequence()
                            .ToChunkOfArray(64)
                            .Select(charArray => new string(charArray)));
                case Base64EncodingType.Rrc4880Encoding: // for OpenPGP Radix-64
                    var crc24Value = new ValueHolder<UInt32>();
                    var bodyPart =
                        string.Join(
                            "\r\n",
                            source
                            .GetSequenceWithCrc24(crc24Value)
                            .GetBase64EncodedSequence()
                            .ToChunkOfArray(76)
                            .Select(charArray => new string(charArray)));
                    var crcPart =
                        new string(
                            new[]
                            {
                                (byte)((crc24Value.Value >> 16) & byte.MaxValue),
                                (byte)((crc24Value.Value >> 8) & byte.MaxValue),
                                (byte)((crc24Value.Value >> 0) & byte.MaxValue),
                            }
                            .GetBase64EncodedSequence()
                            .ToArray());
                    return bodyPart + "\r\n=" + crcPart;
                default:
                    throw new ArgumentException();
            }
        }

        public static IEnumerable<byte> DecodeBase64(this string source, Base64EncodingType encodingType = Base64EncodingType.Default)
        {
            switch (encodingType)
            {
                case Base64EncodingType.Rrc4648Encoding: // Default
                    return source.GetBase64DecodedSequence(false, false);
                case Base64EncodingType.Rrc2045Encoding: // for MIME
                    return source.GetBase64DecodedSequence(true, true);
                case Base64EncodingType.Rrc4880Encoding: // for OpenPGP Radix-64
                    var indexOfLastEqualSign = source.LastIndexOf('=');
                    var bodyPart = indexOfLastEqualSign >= 0 ? source.Substring(0, indexOfLastEqualSign) : source;
                    var crcPart = indexOfLastEqualSign >= 0 ? source.Substring(indexOfLastEqualSign) : null;
                    var data = bodyPart.GetBase64DecodedSequence(true, false).ToArray();
                    if (crcPart != null)
                    {
                        var crcByteArray = crcPart.GetSequence(1).GetBase64DecodedSequence(true, false).ToArray();
                        if (crcByteArray.Length != 3)
                            throw new FormatException();
                        var desiredCrc = ((UInt32)crcByteArray[0] << 16) | ((UInt32)crcByteArray[1] << 8) | ((UInt32)crcByteArray[2] << 0);
                        var actualCrc = data.CalculateCrc24();
                        if (actualCrc != desiredCrc)
                            throw new FormatException();
                    }
                    return data;
                default:
                    throw new ArgumentException();
            }
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
                var value1 = byteArray1.ToUInt64LE(index1);
                var value2 = byteArray2.ToUInt64LE(index2);
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

        public static IEnumerable<TinyBitArray> GetBitArraySequence(this IEnumerable<byte> source, int bitCount, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return new BitArraySequenceFromByteSequence(source, bitCount, packingDirection);
        }

        public static IEnumerable<TinyBitArray> GetBitArraySequence(this IEnumerable<byte[]> source, int bitCount, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return source.SelectMany(bytes => bytes).GetBitArraySequence(bitCount, packingDirection);
        }

        public static IEnumerable<TinyBitArray> GetBitArraySequence(this IEnumerable<IReadOnlyArray<byte>> source, int bitCount, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return source.SelectMany(bytes => bytes).GetBitArraySequence(bitCount, packingDirection);
        }

        public static IEnumerable<Byte> GetByteSequence(this IEnumerable<TinyBitArray> source, BitPackingDirection packingDirection = BitPackingDirection.MsbToLsb)
        {
            return new ByteSequenceFromBitArraySequence(source, packingDirection);
        }

        public static Int16 ToInt16LE(this IReadOnlyArray<byte> array, int startIndex) => (Int16)array.ToUInt16LE(startIndex);

        public static UInt16 ToUInt16LE(this IReadOnlyArray<byte> array, int startIndex)
        {
#if DEBUG
            if (sizeof(UInt16) != 2)
                throw new Exception();
#endif
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (startIndex < 0)
                throw new IndexOutOfRangeException();
            if (startIndex + sizeof(UInt16) > array.Length)
                throw new IndexOutOfRangeException();
            return (UInt16)(((UInt16)array[startIndex + 0] << 0) | ((UInt16)array[startIndex + 1] << 8));
        }

        public static Int32 ToInt32LE(this IReadOnlyArray<byte> array, int startIndex) => (Int32)array.ToUInt32LE(startIndex);

        public static UInt32 ToUInt32LE(this IReadOnlyArray<byte> array, int startIndex)
        {
#if DEBUG
            if (sizeof(UInt32) != 4)
                throw new Exception();
#endif
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (startIndex < 0)
                throw new IndexOutOfRangeException();
            if (startIndex + sizeof(UInt32) > array.Length)
                throw new IndexOutOfRangeException();
            return
                ((UInt32)array[startIndex + 0] << 00) |
                ((UInt32)array[startIndex + 1] << 08) |
                ((UInt32)array[startIndex + 2] << 16) |
                ((UInt32)array[startIndex + 3] << 24);
        }

        public static Int64 ToInt64LE(this IReadOnlyArray<byte> array, int startIndex) => (Int64)array.ToUInt64LE(startIndex);

        public static UInt64 ToUInt64LE(this IReadOnlyArray<byte> array, int startIndex)
        {
#if DEBUG
            if (sizeof(UInt64) != 8)
                throw new Exception();
#endif
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (startIndex < 0)
                throw new IndexOutOfRangeException();
            if (startIndex + sizeof(UInt64) > array.Length)
                throw new IndexOutOfRangeException();
            return
                ((UInt64)array[startIndex + 0] << 0) |
                ((UInt64)array[startIndex + 1] << 8) |
                ((UInt64)array[startIndex + 2] << 16) |
                ((UInt64)array[startIndex + 3] << 24) |
                ((UInt64)array[startIndex + 4] << 32) |
                ((UInt64)array[startIndex + 5] << 40) |
                ((UInt64)array[startIndex + 6] << 48) |
                ((UInt64)array[startIndex + 7] << 56);
        }

        public static float ToSingleLE(this IReadOnlyArray<byte> array, int startIndex) => array.GetRawArray().ToSingleLE(startIndex);
        public static double ToDoubleLE(this IReadOnlyArray<byte> array, int startIndex) => array.GetRawArray().ToDoubleLE(startIndex);
        public static Int16 ToInt16LE(this byte[] array, int startIndex) => array.AsReadOnly().ToInt16LE(startIndex);
        public static UInt16 ToUInt16LE(this byte[] array, int startIndex) => array.AsReadOnly().ToUInt16LE(startIndex);
        public static Int32 ToInt32LE(this byte[] array, int startIndex) => array.AsReadOnly().ToInt32LE(startIndex);
        public static UInt32 ToUInt32LE(this byte[] array, int startIndex) => array.AsReadOnly().ToUInt32LE(startIndex);
        public static Int64 ToInt64LE(this byte[] array, int startIndex) => array.AsReadOnly().ToInt64LE(startIndex);
        public static UInt64 ToUInt64LE(this byte[] array, int startIndex) => array.AsReadOnly().ToUInt64LE(startIndex);

        public static Single ToSingleLE(this byte[] array, int startIndex)
        {
#if DEBUG
            if (sizeof(Single) != 4)
                throw new Exception();
#endif
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (startIndex < 0)
                throw new IndexOutOfRangeException();
            if (startIndex + sizeof(Single) > array.Length)
                throw new IndexOutOfRangeException();
            if (BitConverter.IsLittleEndian)
                return BitConverter.ToSingle(array, startIndex);
            else
            {
                return
                    BitConverter.ToSingle(new[]
                    {
                        array[startIndex + 3],
                        array[startIndex + 2],
                        array[startIndex + 1],
                        array[startIndex + 0],
                    }, 0);
            }
        }

        public static Double ToDoubleLE(this byte[] array, int startIndex)
        {
#if DEBUG
            if (sizeof(Double) != 8)
                throw new Exception();
#endif
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (startIndex < 0)
                throw new IndexOutOfRangeException();
            if (startIndex + sizeof(Double) > array.Length)
                throw new IndexOutOfRangeException();
            if (BitConverter.IsLittleEndian)
                return BitConverter.ToDouble(array, startIndex);
            else
            {
                return
                    BitConverter.ToDouble(new[]
                    {
                        array[startIndex + 7],
                        array[startIndex + 6],
                        array[startIndex + 5],
                        array[startIndex + 4],
                        array[startIndex + 3],
                        array[startIndex + 2],
                        array[startIndex + 1],
                        array[startIndex + 0],
                    }, 0);
            }
        }

        public static Int16 ToInt16BE(this IReadOnlyArray<byte> array, int startIndex) => (Int16)array.ToUInt16BE(startIndex);

        public static UInt16 ToUInt16BE(this IReadOnlyArray<byte> array, int startIndex)
        {
#if DEBUG
            if (sizeof(UInt16) != 2)
                throw new Exception();
#endif
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (startIndex < 0)
                throw new IndexOutOfRangeException();
            if (startIndex + sizeof(UInt16) > array.Length)
                throw new IndexOutOfRangeException();
            return (UInt16)(((UInt16)array[0] << 8) | ((UInt16)array[1] << 0));
        }

        public static Int32 ToInt32BE(this IReadOnlyArray<byte> array, int startIndex) => (Int32)array.ToUInt32BE(startIndex);

        public static UInt32 ToUInt32BE(this IReadOnlyArray<byte> array, int startIndex)
        {
#if DEBUG
            if (sizeof(UInt32) != 4)
                throw new Exception();
#endif
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (startIndex < 0)
                throw new IndexOutOfRangeException();
            if (startIndex + sizeof(UInt32) > array.Length)
                throw new IndexOutOfRangeException();
            return
                ((UInt32)array[startIndex + 0] << 24) |
                ((UInt32)array[startIndex + 1] << 16) |
                ((UInt32)array[startIndex + 2] << 08) |
                ((UInt32)array[startIndex + 3] << 00);
        }

        public static Int64 ToInt64BE(this IReadOnlyArray<byte> array, int startIndex) => (Int64)array.ToUInt64BE(startIndex);

        public static UInt64 ToUInt64BE(this IReadOnlyArray<byte> array, int startIndex)
        {
#if DEBUG
            if (sizeof(UInt64) != 8)
                throw new Exception();
#endif
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (startIndex < 0)
                throw new IndexOutOfRangeException();
            if (startIndex + sizeof(UInt64) > array.Length)
                throw new IndexOutOfRangeException();
            return
                ((UInt64)array[startIndex + 0] << 56) |
                ((UInt64)array[startIndex + 1] << 48) |
                ((UInt64)array[startIndex + 2] << 40) |
                ((UInt64)array[startIndex + 3] << 32) |
                ((UInt64)array[startIndex + 4] << 24) |
                ((UInt64)array[startIndex + 5] << 16) |
                ((UInt64)array[startIndex + 6] << 08) |
                ((UInt64)array[startIndex + 7] << 00);
        }

        public static float ToSingleBE(this IReadOnlyArray<byte> array, int startIndex) => array.GetRawArray().ToSingleBE(startIndex);
        public static double ToDoubleBE(this IReadOnlyArray<byte> array, int startIndex) => array.GetRawArray().ToDoubleBE(startIndex);
        public static Int16 ToInt16BE(this byte[] array, int startIndex) => array.AsReadOnly().ToInt16BE(startIndex);
        public static UInt16 ToUInt16BE(this byte[] array, int startIndex) => array.AsReadOnly().ToUInt16BE(startIndex);
        public static Int32 ToInt32BE(this byte[] array, int startIndex) => array.AsReadOnly().ToInt32BE(startIndex);
        public static UInt32 ToUInt32BE(this byte[] array, int startIndex) => array.AsReadOnly().ToUInt32BE(startIndex);
        public static Int64 ToInt64BE(this byte[] array, int startIndex) => array.AsReadOnly().ToInt64BE(startIndex);
        public static UInt64 ToUInt64BE(this byte[] array, int startIndex) => array.AsReadOnly().ToUInt64BE(startIndex);

        public static Single ToSingleBE(this byte[] array, int startIndex)
        {
#if DEBUG
            if (sizeof(Single) != 4)
                throw new Exception();
#endif
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (startIndex < 0)
                throw new IndexOutOfRangeException();
            if (startIndex + sizeof(Single) > array.Length)
                throw new IndexOutOfRangeException();
            if (BitConverter.IsLittleEndian)
            {
                return
                    BitConverter.ToSingle(new[]
                    {
                        array[startIndex + 3],
                        array[startIndex + 2],
                        array[startIndex + 1],
                        array[startIndex + 0],
                    }, 0);
            }
            else
                return BitConverter.ToSingle(array, startIndex);
        }

        public static Double ToDoubleBE(this byte[] array, int startIndex)
        {
#if DEBUG
            if (sizeof(Double) != 8)
                throw new Exception();
#endif
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (startIndex < 0)
                throw new IndexOutOfRangeException();
            if (startIndex + sizeof(Double) > array.Length)
                throw new IndexOutOfRangeException();
            if (BitConverter.IsLittleEndian)
            {
                return
                    BitConverter.ToDouble(new[]
                    {
                        array[startIndex + 7],
                        array[startIndex + 6],
                        array[startIndex + 5],
                        array[startIndex + 4],
                        array[startIndex + 3],
                        array[startIndex + 2],
                        array[startIndex + 1],
                        array[startIndex + 0],
                    }, 0);
            }
            else
                return BitConverter.ToDouble(array, startIndex);
        }

        public static string ToFriendlyString(this IReadOnlyArray<byte> value) => value.ToFriendlyString(0, value.Length);
        public static string ToFriendlyString(this IReadOnlyArray<byte> value, int startIndex) => value.ToFriendlyString(startIndex, value.Length - startIndex);

        public static string ToFriendlyString(this IReadOnlyArray<byte> array, int startIndex, int length)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (startIndex < 0)
                throw new IndexOutOfRangeException();
            if (startIndex > array.Length)
                throw new IndexOutOfRangeException();
            if (length < 0)
                throw new ArgumentException();
            if (startIndex + length > array.Length)
                throw new IndexOutOfRangeException();

            var sb = new StringBuilder();
            var isFirst = true;
            for (var index = 0; index < length; ++index)
            {
                if (isFirst == false)
                    sb.Append("-");
                sb.Append(array[startIndex + index].ToString("x2"));
                isFirst = false;
            }
            return sb.ToString();
        }

        public static string ToFriendlyString(this byte[] array) => array.AsReadOnly().ToFriendlyString();
        public static string ToFriendlyString(this byte[] array, int startIndex) => array.AsReadOnly().ToFriendlyString(startIndex);
        public static string ToFriendlyString(this byte[] array, int startIndex, int length) => array.AsReadOnly().ToFriendlyString(startIndex, length);
        public static Encoding GuessWhichEncoding(this byte[] bytes) => bytes.AsReadOnly().GuessWhichEncoding();

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
                        return Encoding.GetEncoding("iso-2022-jp");//JIS_0208 1978
                    else if (b2 == _byteDollar && b3 == _byteB)
                        return Encoding.GetEncoding("iso-2022-jp");//JIS_0208 1983
                    else if (b2 == _byteOpenParenthesis && b3.IsAnyOf(_byteB, _byteJ))
                        return Encoding.GetEncoding("iso-2022-jp");//JIS_ASC
                    else if (b2 == _byteOpenParenthesis && b3 == _byteI)
                        return Encoding.GetEncoding("iso-2022-jp");//JIS_KANA
                    if (i < len - 3)
                    {
                        var b4 = bytes[i + 3];
                        if (b2 == _byteDollar &&
                            b3 == _byteOpenParenthesis &&
                            b4 == _byteD)
                            return Encoding.GetEncoding("iso-2022-jp");//JIS_0212
                        if (i < len - 5 &&
                            b2 == _byteAmpersand &&
                            b3 == _byteAtMark &&
                            b4 == _byteEscape &&
                            bytes[i + 4] == _byteDollar &&
                            bytes[i + 5] == _byteB)
                            return Encoding.GetEncoding("iso-2022-jp");//JIS_0208 1990
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
                return Encoding.GetEncoding("euc-jp"); // euc
            else if (count_shift_jis > count_euc && count_shift_jis > count_utf8)
                return Encoding.GetEncoding("shift_jis");// shift_jis
            else if (count_utf8 > count_euc && count_utf8 > count_shift_jis)
                return Encoding.UTF8; // utf8
            else
                return null;
        }

        private static IEnumerable<char> InternalGetBase64EncodedSequence(IEnumerable<byte> source, char char62, char char63)
        {
            return
                source
                .ToChunkOfReadOnlyArray(3)
                .SelectMany(bytes =>
                {
                    switch (bytes.Length)
                    {
                        case 1:
                            return
                                new[]
                                {
                                    ToBase64Character(bytes[0] >> 2, char62, char63),
                                    ToBase64Character((bytes[0] << 4) & 0x3f, char62, char63),
                                    '=',
                                    '=',
                                };
                        case 2:
                            return
                                new[]
                                {
                                    ToBase64Character(bytes[0] >> 2, char62, char63),
                                    ToBase64Character(((bytes[0] << 4) | ( bytes[1] >> 4)) & 0x3f, char62, char63),
                                    ToBase64Character((bytes[1] << 2) & 0x3f, char62, char63),
                                     '=',
                                };
                        case 3:
                            return
                                new[]
                                {
                                    ToBase64Character(bytes[0] >> 2, char62, char63),
                                    ToBase64Character(((bytes[0] << 4) | ( bytes[1] >> 4)) & 0x3f, char62, char63),
                                    ToBase64Character(((bytes[1] << 2) | ( bytes[2] >> 6)) & 0x3f, char62, char63),
                                    ToBase64Character(bytes[2]& 0x3f, char62, char63),
                                };
                        default:
                            throw new Exception();
                    }
                });
        }

        private static IEnumerable<byte> InternalGetBase64DecodedSequence(IEnumerable<char> source, bool ignoreSpace, bool ignoreInvalidCharacter, char char62, char char63)
        {
            return
                source
                .Where(c =>
                {
                    if (c.IsAnyOf('\r', '\n'))
                        return false;
                    else if (char.IsWhiteSpace(c))
                        return ignoreSpace ? true : throw new FormatException();
                    else
                        return true;
                })
                .TakeWhile(c => c != '=')
                .Select(c => FromBase64Character(c, char62, char63))
                .Where(n =>
                    n >= 0
                    ? true
                    : ignoreInvalidCharacter
                        ? false
                        : throw new FormatException())
                .ToChunkOfReadOnlyArray(4)
                .SelectMany(bitArray =>
                {
                    switch (bitArray.Length)
                    {
                        case 2:
                            return new[]
                            {
                                (Byte)((bitArray[0] << 2) | (bitArray[1] >> 4)),
                            };
                        case 3:
                            return new[]
                            {
                                (Byte)((bitArray[0] << 2) | (bitArray[1] >> 4)),
                                (Byte)((bitArray[1] << 4) | (bitArray[2] >> 2)),
                            };
                        case 4:
                            return new[]
                            {
                                (Byte)((bitArray[0] << 2) | (bitArray[1] >> 4)),
                                (Byte)((bitArray[1] << 4) | (bitArray[2] >> 2)),
                                (Byte)((bitArray[2] << 6) | (bitArray[3] >> 0)),
                            };
                        default:
                            throw new FormatException();
                    }
                });
        }

        private static char ToBase64Character(int n, char char62, char char63)
        {
            if (n < 0)
                throw new Exception();
            else if (n < 26)
                return (char)('A' + n);
            else if (n < 52)
                return (char)('a' + n - 26);
            else if (n < 62)
                return (char)('0' + n - 52);
            else if (n == 62)
                return char62;
            else if (n == 63)
                return char63;
            else
                throw new Exception();
        }

        private static int FromBase64Character(char c, char char62, char char63)
        {
            if (c.IsBetween('A', 'Z'))
                return c - 'A';
            else if (c.IsBetween('a', 'z'))
                return c - 'a' + 26;
            else if (c.IsBetween('0', '9'))
                return c - '0' + 52;
            else if (c == char62)
                return 62;
            else if (c == char63)
                return 63;
            else
                return -1;
        }
    }
}