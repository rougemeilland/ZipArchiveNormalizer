using System;

namespace Utility.IO.Compression.Lz
{
    class BinTree
        : InWindow, IMatchFinder
    {
        private const UInt32 kHash2Size = 1 << 10;
        private const UInt32 kHash3Size = 1 << 16;
        private const UInt32 kBT2HashSize = 1 << 16;
        private const UInt32 kStartMaxLen = 1;
        private const UInt32 kHash3Offset = kHash2Size;
        private const UInt32 kEmptyHashValue = 0;
        private const UInt32 kMaxValForNormalize = ((UInt32)1 << 31) - 1;

        private static IReadOnlyArray<UInt32> _crcTable;

        private UInt32 _cyclicBufferPos;
        private UInt32 _cyclicBufferSize;
        private UInt32 _matchMaxLen;
        private UInt32[] _son;
        private UInt32[] _hash;
        private UInt32 _cutValue;
        private UInt32 _hashMask;
        private UInt32 _hashSizeSum;
        private bool _hashArray;
        private UInt32 _kNumHashDirectBytes;
        private UInt32 _kMinMatchCheck;
        private UInt32 _kFixHashSize;

        static BinTree()
        {
            var crcTable = new uint[256];
            const uint kPoly = 0xEDB88320;
            for (uint i = 0; i < 256; i++)
            {
                uint r = i;
                for (int j = 0; j < 8; j++)
                    if ((r & 1) != 0)
                        r = (r >> 1) ^ kPoly;
                    else
                        r >>= 1;
                crcTable[i] = r;
            }
            _crcTable = crcTable.AsReadOnly();
        }

        public BinTree()
        {
            _cyclicBufferSize = 0;
            _cutValue = 0xFF;
            _hashSizeSum = 0;
            _hashArray = true;
            _kNumHashDirectBytes = 0;
            _kMinMatchCheck = 4;
            _kFixHashSize = kHash2Size + kHash3Size;

        }

        public void SetType(int numHashBytes)
        {
            _hashArray = (numHashBytes > 2);
            if (_hashArray)
            {
                _kNumHashDirectBytes = 0;
                _kMinMatchCheck = 4;
                _kFixHashSize = kHash2Size + kHash3Size;
            }
            else
            {
                _kNumHashDirectBytes = 2;
                _kMinMatchCheck = 2 + 1;
                _kFixHashSize = 0;
            }
        }

        public override void Init()
        {
            base.Init();
            for (var i = 0U; i < _hashSizeSum; i++)
                _hash[i] = kEmptyHashValue;
            _cyclicBufferPos = 0;
            ReduceOffsets(-1);
        }

        public override void MovePos()
        {
            if (++_cyclicBufferPos >= _cyclicBufferSize)
                _cyclicBufferPos = 0;
            base.MovePos();
            if (Position == kMaxValForNormalize)
                Normalize();
        }

        public void Create(UInt32 historySize, UInt32 keepAddBufferBefore, UInt32 matchMaxLen, UInt32 keepAddBufferAfter)
        {
            if (historySize > kMaxValForNormalize - 256)
                throw new Exception();
            _cutValue = 16 + (matchMaxLen >> 1);

            UInt32 windowReservSize = (historySize + keepAddBufferBefore +
                    matchMaxLen + keepAddBufferAfter) / 2 + 256;

            base.Create(historySize + keepAddBufferBefore, matchMaxLen + keepAddBufferAfter, windowReservSize);

            _matchMaxLen = matchMaxLen;

            UInt32 cyclicBufferSize = historySize + 1;
            if (_cyclicBufferSize != cyclicBufferSize)
                _son = new UInt32[(_cyclicBufferSize = cyclicBufferSize) * 2];

            UInt32 hs = kBT2HashSize;

            if (_hashArray)
            {
                hs = historySize - 1;
                hs |= hs >> 1;
                hs |= hs >> 2;
                hs |= hs >> 4;
                hs |= hs >> 8;
                hs >>= 1;
                hs |= UInt16.MaxValue;
                if (hs > (1 << 24))
                    hs >>= 1;
                _hashMask = hs;
                hs++;
                hs += _kFixHashSize;
            }
            if (hs != _hashSizeSum)
            {
                _hash = new UInt32[_hashSizeSum = hs];

            }
        }

        public UInt32 GetMatches(UInt32[] distances)
        {
            UInt32 lenLimit;
            if (Position + _matchMaxLen <= StreamPosition)
                lenLimit = _matchMaxLen;
            else
            {
                lenLimit = StreamPosition - Position;
                if (lenLimit < _kMinMatchCheck)
                {
                    MovePos();
                    return 0;
                }
            }

            var offset = 0U;
            var matchMinPos = (Position > _cyclicBufferSize) ? (Position - _cyclicBufferSize) : 0U;
            var cur = BufferOffset + Position;
            var maxLen = kStartMaxLen; // to avoid items for len < hashSize;
            UInt32 hashValue;
            UInt32 hash2Value;
            UInt32 hash3Value;
            CalculateHash(cur, out hashValue, out hash2Value, out hash3Value);

            var curMatch = _hash[_kFixHashSize + hashValue];
            if (_hashArray)
            {
                var curMatch2 = _hash[hash2Value];
                var curMatch3 = _hash[kHash3Offset + hash3Value];
                _hash[hash2Value] = Position;
                _hash[kHash3Offset + hash3Value] = Position;
                if (curMatch2 > matchMinPos)
                    if (BufferBase[BufferOffset + curMatch2] == BufferBase[cur])
                    {
                        distances[offset++] = maxLen = 2;
                        distances[offset++] = Position - curMatch2 - 1;
                    }
                if (curMatch3 > matchMinPos)
                    if (BufferBase[BufferOffset + curMatch3] == BufferBase[cur])
                    {
                        if (curMatch3 == curMatch2)
                            offset -= 2;
                        distances[offset++] = maxLen = 3;
                        distances[offset++] = Position - curMatch3 - 1;
                        curMatch2 = curMatch3;
                    }
                if (offset != 0 && curMatch2 == curMatch)
                {
                    offset -= 2;
                    maxLen = kStartMaxLen;
                }
            }

            _hash[_kFixHashSize + hashValue] = Position;

            UInt32 ptr0 = (_cyclicBufferPos << 1) + 1;
            UInt32 ptr1 = (_cyclicBufferPos << 1);

            UInt32 len0, len1;
            len0 = len1 = _kNumHashDirectBytes;

            if (_kNumHashDirectBytes != 0)
            {
                if (curMatch > matchMinPos)
                {
                    if (BufferBase[BufferOffset + curMatch + _kNumHashDirectBytes] !=
                            BufferBase[cur + _kNumHashDirectBytes])
                    {
                        distances[offset++] = maxLen = _kNumHashDirectBytes;
                        distances[offset++] = Position - curMatch - 1;
                    }
                }
            }

            UInt32 count = _cutValue;

            while (true)
            {
                if (curMatch <= matchMinPos || count-- == 0)
                {
                    _son[ptr0] = _son[ptr1] = kEmptyHashValue;
                    break;
                }
                UInt32 delta = Position - curMatch;
                UInt32 cyclicPos = ((delta <= _cyclicBufferPos) ?
                            (_cyclicBufferPos - delta) :
                            (_cyclicBufferPos - delta + _cyclicBufferSize)) << 1;

                UInt32 pby1 = BufferOffset + curMatch;
                UInt32 len = Math.Min(len0, len1);
                if (BufferBase[pby1 + len] == BufferBase[cur + len])
                {
                    while (++len != lenLimit)
                        if (BufferBase[pby1 + len] != BufferBase[cur + len])
                            break;
                    if (maxLen < len)
                    {
                        distances[offset++] = maxLen = len;
                        distances[offset++] = delta - 1;
                        if (len == lenLimit)
                        {
                            _son[ptr1] = _son[cyclicPos];
                            _son[ptr0] = _son[cyclicPos + 1];
                            break;
                        }
                    }
                }
                if (BufferBase[pby1 + len] < BufferBase[cur + len])
                {
                    _son[ptr1] = curMatch;
                    ptr1 = cyclicPos + 1;
                    curMatch = _son[ptr1];
                    len1 = len;
                }
                else
                {
                    _son[ptr0] = curMatch;
                    ptr0 = cyclicPos;
                    curMatch = _son[ptr0];
                    len0 = len;
                }
            }
            MovePos();
            return offset;
        }

        public void Skip(UInt32 num)
        {
            do
            {
                UInt32 lenLimit;
                if (Position + _matchMaxLen <= StreamPosition)
                    lenLimit = _matchMaxLen;
                else
                {
                    lenLimit = StreamPosition - Position;
                    if (lenLimit < _kMinMatchCheck)
                    {
                        MovePos();
                        continue;
                    }
                }

                var matchMinPos = (Position > _cyclicBufferSize) ? (Position - _cyclicBufferSize) : 0;
                var cur = BufferOffset + Position;

                var hashValue = CalculateHash(cur);

                var curMatch = _hash[_kFixHashSize + hashValue];
                _hash[_kFixHashSize + hashValue] = Position;

                UInt32 ptr0 = (_cyclicBufferPos << 1) + 1;
                UInt32 ptr1 = (_cyclicBufferPos << 1);

                UInt32 len0, len1;
                len0 = len1 = _kNumHashDirectBytes;

                UInt32 count = _cutValue;
                while (true)
                {
                    if (curMatch <= matchMinPos || count-- == 0)
                    {
                        _son[ptr0] = _son[ptr1] = kEmptyHashValue;
                        break;
                    }

                    UInt32 delta = Position - curMatch;
                    UInt32 cyclicPos = ((delta <= _cyclicBufferPos) ?
                                (_cyclicBufferPos - delta) :
                                (_cyclicBufferPos - delta + _cyclicBufferSize)) << 1;

                    UInt32 pby1 = BufferOffset + curMatch;
                    UInt32 len = Math.Min(len0, len1);
                    if (BufferBase[pby1 + len] == BufferBase[cur + len])
                    {
                        while (++len != lenLimit)
                            if (BufferBase[pby1 + len] != BufferBase[cur + len])
                                break;
                        if (len == lenLimit)
                        {
                            _son[ptr1] = _son[cyclicPos];
                            _son[ptr0] = _son[cyclicPos + 1];
                            break;
                        }
                    }
                    if (BufferBase[pby1 + len] < BufferBase[cur + len])
                    {
                        _son[ptr1] = curMatch;
                        ptr1 = cyclicPos + 1;
                        curMatch = _son[ptr1];
                        len1 = len;
                    }
                    else
                    {
                        _son[ptr0] = curMatch;
                        ptr0 = cyclicPos;
                        curMatch = _son[ptr0];
                        len0 = len;
                    }
                }
                MovePos();
            }
            while (--num != 0);
        }

        private void NormalizeLinks(UInt32[] items, UInt32 numItems, UInt32 subValue)
        {
            for (UInt32 i = 0; i < numItems; i++)
            {
                UInt32 value = items[i];
                if (value <= subValue)
                    value = kEmptyHashValue;
                else
                    value -= subValue;
                items[i] = value;
            }
        }

        private void Normalize()
        {
            UInt32 subValue = Position - _cyclicBufferSize;
            NormalizeLinks(_son, _cyclicBufferSize * 2, subValue);
            NormalizeLinks(_hash, _hashSizeSum, subValue);
            ReduceOffsets((Int32)subValue);
        }

        private UInt32 CalculateHash(UInt32 cur)
        {
            if (_hashArray)
            {
                UInt32 temp = _crcTable[BufferBase[cur]] ^ BufferBase[cur + 1];
                UInt32 hash2Value = temp & (kHash2Size - 1);
                _hash[hash2Value] = Position;
                temp ^= ((UInt32)(BufferBase[cur + 2]) << 8);
                UInt32 hash3Value = temp & (kHash3Size - 1);
                _hash[kHash3Offset + hash3Value] = Position;
                return (temp ^ (_crcTable[BufferBase[cur + 3]] << 5)) & _hashMask;
            }
            else
                return BufferBase[cur] ^ ((UInt32)(BufferBase[cur + 1]) << 8);
        }

        private void CalculateHash(UInt32 cur, out UInt32 hashValue, out UInt32 hash2Value, out UInt32 hash3Value)
        {
            if (_hashArray)
            {
                UInt32 temp = _crcTable[BufferBase[cur]] ^ BufferBase[cur + 1];
                hash2Value = temp & (kHash2Size - 1);
                temp ^= ((UInt32)(BufferBase[cur + 2]) << 8);
                hash3Value = temp & (kHash3Size - 1);
                hashValue = (temp ^ (_crcTable[BufferBase[cur + 3]] << 5)) & _hashMask;
            }
            else
            {
                hashValue = BufferBase[cur] ^ ((UInt32)(BufferBase[cur + 1]) << 8);
                hash2Value = 0U;
                hash3Value = 0U;
            }
        }

    }
}
