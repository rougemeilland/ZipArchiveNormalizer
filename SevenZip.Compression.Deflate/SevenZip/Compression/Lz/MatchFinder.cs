// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using System.Numerics;
using Utility;
using Utility.IO;

namespace SevenZip.Compression.Lz
{
    abstract class MatchFinder
        : IMatchFinder
    {
        private const bool _directInput = false; // Set to true when encoding from memory to memory
        //private Int32 _directInputRem; // Used only if _directInput is true
        //private const UInt64 _expectedDataSize = UInt64.MaxValue; // Customizable only for LZMA2
        //private const bool _bigHash = false; // Only referenced for multithreading

        private readonly bool _btMode;
        private readonly UInt32 _matchMaxLen;
        private readonly Byte[] _bufferBody;
        private readonly UInt32 _blockSize;
        private readonly ArrayPointer<Byte> _bufferBase;
        private readonly ArrayPointer<Byte> _bufferLimit;
        private readonly UInt32 _keepSizeBefore;
        private readonly UInt32 _keepSizeAfter;
        private readonly UInt32 _numHashBytes;
        private readonly UInt32 _historySize;

        private UInt32 pos;
        private UInt32 _streamPos;
        private bool _streamEndWasReached;

        public MatchFinder(bool btMode, UInt32 numHashBytes, UInt32 historySize, UInt32 keepAddBufferBefore, UInt32 matchMaxLen, UInt32 keepAddBufferAfter, UInt32 matchFinderCycles)
        {
            var hashSize = CalculateHashSize(numHashBytes, historySize);
            Pos = 1;
            PosLimit = 0;
            _streamPos = 1;
            LenLimit = 0;
            CyclicBufferPos = 0;
            CyclicBufferSize = historySize + 1;
            _streamEndWasReached = false;
            _btMode = btMode;
            _matchMaxLen = matchMaxLen;
            Hash = new UInt32[hashSize];
            HashMask = hashSize - 1;
            Son = new UInt32[_btMode ? CyclicBufferSize << 1 : CyclicBufferSize];
            CutValue = matchFinderCycles;
            _keepSizeBefore = historySize + keepAddBufferBefore + 1;
            _keepSizeAfter = (keepAddBufferAfter + matchMaxLen).Maximum(numHashBytes);
            _numHashBytes = numHashBytes;
            _blockSize = GetBlockSize(historySize);
            _bufferBody = new Byte[_blockSize];
            _bufferBase = _bufferBody.GetPointer();
            _bufferLimit = _bufferBody.GetPointer(_bufferBody.Length);
            BufferPointer = _bufferBody.GetPointer();
            //_directInputRem = 0; // Used only if _directInput is true
            _historySize = historySize;
        }

        public ReadOnlyArrayPointer<Byte> CurrentPos => BufferPointer;

        public UInt32 NumAvailableBytes => _streamPos - Pos;

        public UInt32 BlockSize => _blockSize;

        public void Initialize(IBasicInputByteStream inStream)
        {
            ReadBlock(inStream);
            CyclicBufferPos = Pos;
            SetLimits();
        }

        // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
        public abstract UInt32 GetMatches(IBasicInputByteStream inStream, Span<UInt32> distances);
        public abstract void Skip(IBasicInputByteStream inStream, UInt32 num);

        public UInt32 GetMatchLen(Int32 index, UInt32 distance, UInt32 limit)
        {
            {
#if DEBUG
                var lowerBoundOfBufferIndex = (Int64)(BufferPointer - _bufferBase) + index - (distance + 1);
                if (lowerBoundOfBufferIndex < 0)
                    throw new Exception();
#endif
                var upperBoundOfBufferIndex = (Int64)(BufferPointer - _bufferBase) + index + limit;
                if (_streamEndWasReached && upperBoundOfBufferIndex > _streamPos)
                    limit = _streamPos - (UInt32)(BufferPointer - _bufferBase + index);
            }
            ++distance;
            var i = 0;
            while (i < limit && BufferPointer[index + i] == BufferPointer[(Int32)(index + i - distance)])
                ++i;
            return (UInt32)i;
        }

        protected UInt32 PosLimit { get; private set; }
        protected UInt32 LenLimit { get; private set; }
        protected UInt32 CyclicBufferPos { get; set; }
        protected UInt32 CyclicBufferSize { get; }
        protected UInt32[] Hash { get; }
        protected UInt32[] Son { get; }
        protected UInt32 HashMask { get; }
        protected UInt32 CutValue { get; }
        protected ArrayPointer<Byte> BufferPointer { get; set; }
        protected UInt32 Pos { get => pos; set => pos = value; }

        protected void MovePos(IBasicInputByteStream inStream)
        {
            ++CyclicBufferPos;
            ++BufferPointer;
            if (++Pos >= PosLimit)
                CheckLimits(inStream);
        }

        protected void CheckLimits(IBasicInputByteStream inStream)
        {
            if (_keepSizeAfter == _streamPos - Pos)
            {
                MoveBlock();
                ReadBlock(inStream);
            }
            if (Pos == LzConstants.kMaxValForNormalize && _streamPos - Pos >= _numHashBytes)
            {
                var subValue = Pos - _historySize - 1;
                Pos -= subValue;
                _streamPos -= subValue;
                Normalize3(subValue);
            }
            if (CyclicBufferPos == CyclicBufferSize)
                CyclicBufferPos = 0;
            SetLimits();
        }

        protected virtual void Normalize3(UInt32 subValue)
        {
            NormalizeHash(subValue, Hash);
            NormalizeHash(subValue, Son);
        }

        protected static void NormalizeHash(UInt32 subValue, UInt32[] hash)
        {
            for (var index = 0; index < hash.Length; ++index)
            {
                var value = hash[index];
                hash[index] =
                    value > subValue
                    ? value - subValue
                    : 0;
            }
        }

        private UInt32 GetBlockSize(UInt32 historySize)
        {
            var blockSize = _keepSizeBefore + _keepSizeAfter;
            if (!_keepSizeBefore.IsBetween(historySize, blockSize))
                return 0;
            var rem = UInt32.MaxValue - blockSize;
            var reserve =
                (blockSize >> (blockSize < (1U << 30) ? 1 : 2))
                + (1 << 12)
                + 2;
            if (rem < (1 << 24))
                return 0;
            if (reserve >= rem)
                blockSize = UInt32.MaxValue;
            else
                blockSize += reserve;
            return blockSize;
        }

        private static UInt32 CalculateHashSize(UInt32 numHashBytes, UInt32 historySize)
        {
            switch (numHashBytes)
            {
                case 2:
                    return 1U << 16;
                case 3:
                    if (historySize <= 1U << 16)
                        return 1U << 16;
                    --historySize;
                    if (historySize >= 1U << 24)
                        return 1U << 24;
                    return 1U << (31 - BitOperations.LeadingZeroCount(historySize));
                case 4:
                    if (historySize > 0)
                        --historySize;
                    if (historySize <= 1U << 16)
                        return 1U << 16;
                    {
                        var bitLength = 31 - BitOperations.LeadingZeroCount(historySize);
                        if (bitLength > 24)
                            --bitLength;
                        return 1U << bitLength;
                    }
                default:
                    if (historySize > 0)
                        --historySize;
                    if (historySize <= 1U << 18)
                        return 1U << 18;
                    {
                        var bitLength = 31 - BitOperations.LeadingZeroCount(historySize);
                        if (bitLength > 24)
                            --bitLength;
                        return 1U << bitLength;
                    }
            }
        }

        private void MoveBlock()
        {
            if (!_directInput
                && !_streamEndWasReached
                && _bufferLimit - BufferPointer <= _keepSizeAfter)
            {
                var offset = checked((UInt32)(BufferPointer - _bufferBase - _keepSizeBefore));
                _bufferBody.CopyTo(offset, _bufferBody, 0, _streamPos - Pos);
                BufferPointer = _bufferBase + _keepSizeBefore;
            }
        }

        private void ReadBlock(IBasicInputByteStream inStream)
        {
            if (_streamEndWasReached)
                return;
#if false // _directInput is always false.
            if (_directInput)
            {
                var curSize = UInt32.MaxValue - _streamPos - _pos;
                if (curSize > _directInputRem)
                    curSize = (UInt32)_directInputRem;
                _directInputRem -= curSize;
                _streamPos += curSize;
                if (_directInputRem == 0)
                    _streamEndWasReached = true;
                return;
            }
#endif
            while (true)
            {
                var dest = BufferPointer + _streamPos - Pos;
                var size = _bufferLimit - dest;
                if (size == 0)
                    return;
                size = inStream.Read(dest.GetSpan(size));
                if (size <= 0)
                {
                    _streamEndWasReached = true;
                    return;
                }
                _streamPos += (UInt32)size;
                if (_streamPos - Pos > _keepSizeAfter)
                    return;
            }
        }

        private void SetLimits()
        {
            var n = LzConstants.kMaxValForNormalize - Pos;
            if (n == 0)
                n = UInt32.MaxValue;
            n = n.Minimum(CyclicBufferSize - CyclicBufferPos);
            var k = _streamPos - Pos;
            var ksa = _keepSizeAfter;
            if (k > ksa)
            {
                k -= ksa;
                LenLimit = _matchMaxLen;
            }
            else if (k >= _matchMaxLen)
            {
                k = k - _matchMaxLen + 1;
                LenLimit = _matchMaxLen;
            }
            else
            {
                LenLimit = k;
                if (k > 0)
                    k = 1;
            }
            PosLimit = Pos + n.Minimum(k);
        }
    }
}
