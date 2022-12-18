// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using SevenZip.Compression.Huffman;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Utility;
using Utility.IO;

namespace SevenZip.Compression.Deflate.Encoder
{
    class InternalDeflateEncoderStream
        : IBasicOutputByteStream
    {
        private readonly IBasicOutputByteStream _baseStream;
        private readonly IProgress<UInt64>? _progress;
        private readonly bool _leaveOpen;
        private readonly BitLsb.Encoder outStream;
        private readonly CodeValue[] _values;
        private readonly UInt32 _numFastBytes;
        private readonly bool _fastMode;
        private readonly bool _btMode;
        private readonly UInt16[] _onePosMatchesMemory;
        private readonly UInt16[] _distanceMemory;
        private readonly UInt32 _numPasses;
        private readonly UInt32 _numDivPasses;
        private readonly bool _checkStatic;
        private readonly bool _isMultiPass;
        private readonly UInt32 _valueBlockSize;
        private readonly UInt32 _numLenCombinations;
        private readonly UInt32 _matchMaxLen;
        // TODO: 正常動作を確認後、SpanやMemoryを配列に置き換えることを考慮に入れて高速化を試みる。
        private readonly ReadOnlyMemory<Byte> _lenStart;
        private readonly ReadOnlyMemory<Byte> _lenDirectBits;
        private readonly bool _deflate64Mode;
        private readonly Byte[] _levelLevels;
        private readonly Byte[] _literalPrices;
        private readonly Byte[] _lenPrices;
        private readonly Byte[] _posPrices;
        private readonly Levels _newLevels;
        private readonly UInt32[] _mainFreqs;
        private readonly UInt32[] _distFreqs;
        private readonly UInt32[] _mainCodes;
        private readonly UInt32[] _distCodes;
        private readonly UInt32[] _levelCodes;
        private readonly Byte[] _levelLens;
        private readonly Tables[] _tables;
        private readonly Optimal[] _optimum;
        private readonly UInt32 _matchFinderCycles;
        private readonly Lz.IMatchFinder _lzInWindow;
        private readonly ByteIOQueue _intermediateBuffer;
        private readonly IBasicInputByteStream _intermediateBufferReader;
        private readonly IBasicOutputByteStream _intermediateBufferWriter;

        private bool _isDisposed;
        private ArrayPointer<UInt16> _matchDistances;
        private UInt32 _pos;
        private UInt32 _numLitLenLevels;
        private UInt32 _numDistLevels;
        private UInt32 _numLevelCodes;
        private UInt32 _valueIndex;
        private bool _secondPass;
        private UInt32 _additionalOffset;
        private UInt32 _optimumEndIndex;
        private UInt32 _optimumCurrentIndex;
        private UInt32 _blockSizeRes;
        private UInt64 _nowPos;
        private bool _isFirstBlock;

        public InternalDeflateEncoderStream(bool deflate64Mode, IBasicOutputByteStream baseStream, InternalDeflateEncoderProperties properties, IProgress<UInt64>? progress, bool leaveOpen)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));
                if (properties is null)
                    throw new ArgumentNullException(nameof(properties));

                _isDisposed = false;
                _baseStream = baseStream;
                _progress = progress;
                _leaveOpen = leaveOpen;
                outStream = new BitLsb.Encoder();
                _values = new CodeValue[DeflateEncoderConstants.kMaxUncompressedBlockSize];
                _values.FillArray(_ => new CodeValue());
                _numFastBytes = properties.NumFastBytes;
                _fastMode = !properties.Algorithm;
                _btMode = properties.MatchFinder == MatchFinderType.BT3ZIP;
                _pos = 0;
                _numDivPasses = properties.NumPasses;
                if (_numDivPasses == 0)
                    _numDivPasses = 1;
                if (_numDivPasses == 1)
                    _numPasses = 1;
                else if (_numDivPasses <= DeflateEncoderConstants.kNumDivPassesMax)
                    _numPasses = 2;
                else
                {
                    _numPasses = 2 + (_numDivPasses - DeflateEncoderConstants.kNumDivPassesMax);
                    _numDivPasses = DeflateEncoderConstants.kNumDivPassesMax;
                }
                _checkStatic = _numPasses != 1 || _numDivPasses != 1;
                _isMultiPass = _checkStatic || _numPasses != 1 || _numDivPasses != 1;
                if (_isMultiPass)
                {
                    _onePosMatchesMemory = new UInt16[DeflateEncoderConstants.kMatchArraySize];
                    _distanceMemory = Array.Empty<UInt16>();
                    _matchDistances = _distanceMemory.GetPointer();
                }
                else
                {
                    _onePosMatchesMemory = Array.Empty<UInt16>();
                    _distanceMemory = new UInt16[(DeflateConstants.kMatchMaxLen + 2) * 2];
                    _matchDistances = _distanceMemory.GetPointer();
                }
                _valueBlockSize = (7 << 10) + (1 << 12) * _numDivPasses;
                _numLenCombinations = deflate64Mode ? DeflateConstants.kNumLenSymbols64 : DeflateConstants.kNumLenSymbols32;
                _matchMaxLen = deflate64Mode ? DeflateConstants.kMatchMaxLen64 : DeflateConstants.kMatchMaxLen32;
                _lenStart = deflate64Mode ? DeflateConstants.kLenStart64 : DeflateConstants.kLenStart32;
                _lenDirectBits = deflate64Mode ? DeflateConstants.kLenDirectBits64 : DeflateConstants.kLenDirectBits32;
                _deflate64Mode = deflate64Mode;
                _levelLevels = new Byte[DeflateConstants.kLevelTableSize];
                _numLitLenLevels = 0;
                _numDistLevels = 0;
                _numLevelCodes = 0;
                _valueIndex = 0;
                _secondPass = false;
                _additionalOffset = 0;
                _optimumEndIndex = 0;
                _optimumCurrentIndex = 0;
                _literalPrices = new Byte[256];
                _lenPrices = new Byte[DeflateConstants.kNumLenSymbolsMax];
                _posPrices = new Byte[DeflateConstants.kDistTableSize64];
                _newLevels = new Levels();
                _mainFreqs = new UInt32[DeflateConstants.kFixedMainTableSize];
                _distFreqs = new UInt32[DeflateConstants.kDistTableSize64];
                _mainCodes = new UInt32[DeflateConstants.kFixedMainTableSize];
                _distCodes = new UInt32[DeflateConstants.kDistTableSize64];
                _levelCodes = new UInt32[DeflateConstants.kLevelTableSize];
                _levelLens = new Byte[DeflateConstants.kLevelTableSize];
                _blockSizeRes = 0;
                _tables = new Tables[DeflateEncoderConstants.kNumTables];
                _tables.FillArray(_ => new Tables());
                _optimum = new Optimal[DeflateEncoderConstants.kNumOpts];
                _optimum.FillArray(_ => new Optimal());
                _matchFinderCycles = properties.MatchFinderCycles;
                _lzInWindow =
                    _btMode
                    ? new Lz.Bt3ZipMatchFinder(
                        _deflate64Mode ? DeflateEncoderConstants.kHistorySize64 : DeflateEncoderConstants.kHistorySize32,
                        DeflateEncoderConstants.kNumOpts + DeflateEncoderConstants.kMaxUncompressedBlockSize,
                        _numFastBytes,
                        _matchMaxLen - _numFastBytes,
                        _matchFinderCycles)
                    : new Lz.Hc3ZipMatchFinder(
                        _deflate64Mode ? DeflateEncoderConstants.kHistorySize64 : DeflateEncoderConstants.kHistorySize32,
                        DeflateEncoderConstants.kNumOpts + DeflateEncoderConstants.kMaxUncompressedBlockSize,
                        _numFastBytes,
                        _matchMaxLen - _numFastBytes,
                        _matchFinderCycles);
                _intermediateBuffer = new ByteIOQueue(_lzInWindow.BlockSize);
                _intermediateBufferReader = _intermediateBuffer.GetReader();
                _intermediateBufferWriter = _intermediateBuffer.GetWriter();
                _nowPos = 0;
                _isFirstBlock = true;
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public Int32 Write(ReadOnlySpan<byte> buffer)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            return InternalWrite(buffer);
        }

        public Task<Int32> WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            return Task.FromResult(InternalWrite(buffer.Span));
        }

        public void Flush()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    InternalFlush();
                    if (!_leaveOpen)
                        _baseStream.Dispose();
                }
                _isDisposed = true;
            }
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (!_isDisposed)
            {
                InternalFlush();
                if (!_leaveOpen)
                    await _baseStream.DisposeAsync().ConfigureAwait(false);
                _isDisposed = true;
            }
        }

        private Int32 InternalWrite(ReadOnlySpan<byte> buffer)
        {
            while (_intermediateBuffer.IsFull)
                EncodeBlock(_intermediateBufferReader, _baseStream);
            return _intermediateBufferWriter.Write(buffer);
        }

        private void InternalFlush()
        {
            try
            {
                _intermediateBufferWriter.Dispose();
                while (EncodeBlock(_intermediateBufferReader, _baseStream)) ;
                _intermediateBufferReader.Dispose();
                _baseStream.Flush();
            }
            catch (Exception)
            {
            }
        }

        private bool EncodeBlock(IBasicInputByteStream inStream, IBasicOutputByteStream outStream)
        {
            var table = _tables[1];
            if (_isFirstBlock)
            {
                _lzInWindow.Initialize(inStream);
                table.Pos = 0;
                table.Initialize();
                _isFirstBlock = false;
            }
            table.BlockSizeRes = DeflateEncoderConstants.kBlockUncompressedSizeThreshold;
            _secondPass = false;
            GetBlockPrice(inStream, 1, _numDivPasses);
            CodeBlock(inStream, outStream, 1, _lzInWindow.NumAvailableBytes <= 0);
            _nowPos += _tables[1].BlockSizeRes;
            try
            {
                _progress?.Report(_nowPos);
            }
            catch (Exception)
            {
            }
            var doContinue = _lzInWindow.NumAvailableBytes > 0;
            if (!doContinue)
                this.outStream.Flush(outStream);
            return doContinue;
        }

        private UInt32 GetBlockPrice(IBasicInputByteStream inStream, UInt32 tableIndex, UInt32 numDivPasses)
        {
            var t = _tables[tableIndex];
            t.StaticMode = false;
            var price = TryDynBlock(inStream, tableIndex, _numPasses);
            t.BlockSizeRes = _blockSizeRes;
            var numValues = _valueIndex;
            var posTemp = _pos;
            var additionalOffsetEnd = _additionalOffset;
            if (_checkStatic && _valueIndex <= DeflateEncoderConstants.kFixedHuffmanCodeBlockSizeMax)
            {
                var fixedPrice = TryFixedBlock(inStream, tableIndex);
                t.StaticMode = fixedPrice < price;
                if (t.StaticMode)
                    price = fixedPrice;
            }
            var storePrice = GetStorePrice(_blockSizeRes, 0);
            t.StoreMode = storePrice <= price;
            if (t.StoreMode)
                price = storePrice;
            t.UseSubBlocks = false;
            if (numDivPasses > 1 && numValues >= DeflateEncoderConstants.kDivideCodeBlockSizeMin)
            {
                var t0 = _tables[tableIndex << 1];
                t0.SetLitLenLevels(t.LitLenLevels);
                t0.SetDistLevels(t.DistLevels);
                t0.BlockSizeRes = t.BlockSizeRes >> 1;
                t0.Pos = t.Pos;
                var subPrice = GetBlockPrice(inStream, tableIndex << 1, numDivPasses - 1);
                var blockSize2 = t.BlockSizeRes - t0.BlockSizeRes;
                if (t0.BlockSizeRes >= DeflateEncoderConstants.kDivideBlockSizeMin && blockSize2 >= DeflateEncoderConstants.kDivideBlockSizeMin)
                {
                    var t1 = _tables[(tableIndex << 1) + 1];
                    t1.SetLitLenLevels(t.LitLenLevels);
                    t1.SetDistLevels(t.DistLevels);
                    t1.BlockSizeRes = blockSize2;
                    t1.Pos = _pos;
                    _additionalOffset -= t0.BlockSizeRes;
                    subPrice += GetBlockPrice(inStream, (tableIndex << 1) + 1, numDivPasses - 1);
                    t.UseSubBlocks = subPrice < price;
                    if (t.UseSubBlocks)
                        price = subPrice;
                }
            }
            _additionalOffset = additionalOffsetEnd;
            _pos = posTemp;
            return price;
        }

        private void CodeBlock(IBasicInputByteStream inStream, IBasicOutputByteStream outStream, UInt32 tableIndex, bool finalBlock)
        {
            var t = _tables[tableIndex];
            if (t.UseSubBlocks)
            {
                CodeBlock(inStream, outStream, tableIndex << 1, false);
                CodeBlock(inStream, outStream, (tableIndex << 1) + 1, finalBlock);
            }
            else
            {
                if (t.StoreMode)
                    WriteStoreBlock(outStream, t.BlockSizeRes, _additionalOffset, finalBlock);
                else
                {
                    WriteBits(outStream, finalBlock ? DeflateConstants.FinalBlockField.kFinalBlock : DeflateConstants.FinalBlockField.kNotFinalBlock, DeflateConstants.kFinalBlockFieldSize);
                    if (t.StaticMode)
                    {
                        WriteBits(outStream, DeflateConstants.BlockType.kFixedHuffman, DeflateConstants.kBlockTypeFieldSize);
                        TryFixedBlock(inStream, tableIndex);
                        var kMaxStaticHuffLen = 9;
                        for (var i = 0; i < DeflateConstants.kFixedMainTableSize; i++)
                            _mainFreqs[i] = 1U << (kMaxStaticHuffLen - _newLevels.LitLenLevels[i]);
                        for (var i = 0; i < DeflateConstants.kFixedDistTableSize; i++)
                            _distFreqs[i] = 1U << (kMaxStaticHuffLen - _newLevels.DistLevels[i]);
                        MakeTables((UInt32)kMaxStaticHuffLen);
                    }
                    else
                    {
                        if (_numDivPasses > 1 || _checkStatic)
                            TryDynBlock(inStream, tableIndex, 1);
                        WriteBits(outStream, DeflateConstants.BlockType.kDynamicHuffman, DeflateConstants.kBlockTypeFieldSize);
                        WriteBits(outStream, _numLitLenLevels - DeflateConstants.kNumLitLenCodesMin, DeflateConstants.kNumLenCodesFieldSize);
                        WriteBits(outStream, _numDistLevels - DeflateConstants.kNumDistCodesMin, DeflateConstants.kNumDistCodesFieldSize);
                        WriteBits(outStream, _numLevelCodes - DeflateConstants.kNumLevelCodesMin, DeflateConstants.kNumLevelCodesFieldSize);
                        for (var i = 0; i < _numLevelCodes; i++)
                            WriteBits(outStream, _levelLevels[i], DeflateConstants.kLevelFieldSize);
                        Huffman_ReverseBits(_levelCodes, _levelLens, DeflateConstants.kLevelTableSize);
                        LevelTableCode(outStream, _newLevels.LitLenLevels, _numLitLenLevels, _levelLens, _levelCodes);
                        LevelTableCode(outStream, _newLevels.DistLevels, _numDistLevels, _levelLens, _levelCodes);
                    }
                    WriteBlock(outStream);
                }
                _additionalOffset -= t.BlockSizeRes;
            }
        }

        private UInt32 TryDynBlock(IBasicInputByteStream inStream, UInt32 tableIndex, UInt32 numPasses)
        {
            var t = _tables[tableIndex];
            _blockSizeRes = t.BlockSizeRes;
            var posTemp = t.Pos;
            SetPrices(t);

            for (var p = 0; p < numPasses; p++)
            {
                _pos = posTemp;
                TryBlock(inStream);
                var numHuffBits =
                    _valueIndex > 18000
                        ? 12U
                        : _valueIndex > 7000
                            ? 11U
                            : _valueIndex > 2000
                                ? 10U
                                : 9U;
                MakeTables(numHuffBits);
                SetPrices(_newLevels);
            }
            t.SetLitLenLevels(_newLevels.LitLenLevels);
            t.SetDistLevels(_newLevels.DistLevels);
            _numLitLenLevels = DeflateConstants.kMainTableSize;

            while (_numLitLenLevels > DeflateConstants.kNumLitLenCodesMin && _newLevels.LitLenLevels[(Int32)_numLitLenLevels - 1] == 0)
                _numLitLenLevels--;

            _numDistLevels = DeflateConstants.kDistTableSize64;
            while (_numDistLevels > DeflateConstants.kNumDistCodesMin && _newLevels.DistLevels[(Int32)_numDistLevels - 1] == 0)
                _numDistLevels--;

            Span<UInt32> levelFreqs = stackalloc UInt32[DeflateConstants.kLevelTableSize];
            levelFreqs.Clear();

            LevelTableDummy(_newLevels.LitLenLevels, _numLitLenLevels, levelFreqs);
            LevelTableDummy(_newLevels.DistLevels, _numDistLevels, levelFreqs);

            levelFreqs.AsReadOnly().HuffmanGenerate(_levelCodes, _levelLens, DeflateConstants.kLevelTableSize, DeflateEncoderConstants.kMaxLevelBitLength);

            _numLevelCodes = DeflateConstants.kNumLevelCodesMin;
            var kCodeLengthAlphabetOrder = DeflateConstants.kCodeLengthAlphabetOrder.Span;
            for (var i = 0; i < DeflateConstants.kLevelTableSize; i++)
            {
                var level = _levelLens[kCodeLengthAlphabetOrder[i]];
                if (level > 0 && i >= _numLevelCodes)
                    _numLevelCodes = (UInt32)i + 1;
                _levelLevels[i] = level;
            }

            return
                GetLzBlockPrice()
                + Huffman_GetPrice_Spec(
                    levelFreqs,
                    _levelLens,
                    DeflateConstants.kLevelTableSize,
                    DeflateConstants.kLevelDirectBits.Span,
                    DeflateConstants.kTableDirectLevels)
                + DeflateConstants.kNumLenCodesFieldSize
                + DeflateConstants.kNumDistCodesFieldSize
                + DeflateConstants.kNumLevelCodesFieldSize
                + _numLevelCodes * DeflateConstants.kLevelFieldSize
                + DeflateConstants.kFinalBlockFieldSize
                + DeflateConstants.kBlockTypeFieldSize;
        }

        private UInt32 TryFixedBlock(IBasicInputByteStream inStream, UInt32 tableIndex)
        {
            var t = _tables[tableIndex];
            _blockSizeRes = t.BlockSizeRes;
            _pos = t.Pos;
            _newLevels.SetFixedLevels();
            SetPrices(_newLevels);
            TryBlock(inStream);
            return
                DeflateConstants.kFinalBlockFieldSize
                + DeflateConstants.kBlockTypeFieldSize
                + GetLzBlockPrice();
        }

        private void TryBlock(IBasicInputByteStream inStream)
        {
            _mainFreqs.ClearArray();
            _distFreqs.ClearArray();
            _valueIndex = 0;
            var blockSize = _blockSizeRes;
            _blockSizeRes = 0;
            var lenSlots = DeflateEncoderConstants.LenSlots.Span;
            while (true)
            {
                if (_optimumCurrentIndex == _optimumEndIndex)
                {
                    if (_pos >= DeflateEncoderConstants.kMatchArrayLimit
                        || _blockSizeRes >= blockSize
                        || (!_secondPass && (_lzInWindow.NumAvailableBytes == 0 || _valueIndex >= _valueBlockSize)))
                        break;
                }

                var (pos, len) = _fastMode ? GetOptimalFast(inStream) : GetOptimal(inStream);
                var codeValue = _values[_valueIndex++];
                if (len >= DeflateConstants.kMatchMinLen)
                {
                    var newLen = (UInt16)(len - DeflateConstants.kMatchMinLen);
                    codeValue.Len = newLen;
                    _mainFreqs[DeflateConstants.kSymbolMatch + lenSlots[newLen]]++;
                    codeValue.Pos = (UInt16)pos;
                    _distFreqs[GetPosSlot(pos)]++;
                }
                else
                {
                    var b = (_lzInWindow.CurrentPos - _additionalOffset)[0];
                    _mainFreqs[b]++;
                    codeValue.SetAsLiteral();
                    codeValue.Pos = b;
                }
                _additionalOffset -= len;
                _blockSizeRes += len;
            }
            _mainFreqs[DeflateConstants.kSymbolEndOfBlock]++;
            _additionalOffset += _blockSizeRes;
            _secondPass = true;
        }

        private (UInt32 Pos, UInt32 Len) GetOptimalFast(IBasicInputByteStream inStream)
        {
            GetMatches(inStream);
            var numDistancePairs = _matchDistances[0];
            if (numDistancePairs == 0)
                return (0, 1);
            var lenMain = (UInt32)_matchDistances[numDistancePairs - 1];
            var backRes = _matchDistances[numDistancePairs];
            MovePos(inStream, lenMain - 1);
            return (backRes, lenMain);
        }

        private (UInt32 Pos, UInt32 Len) GetOptimal(IBasicInputByteStream inStream)
        {
            if (_optimumEndIndex != _optimumCurrentIndex)
            {
                var len = _optimum[_optimumCurrentIndex].PosPrev - _optimumCurrentIndex;
                var backRes = _optimum[_optimumCurrentIndex].BackPrev;
                _optimumCurrentIndex = _optimum[_optimumCurrentIndex].PosPrev;
                return (backRes, len);
            }
            _optimumCurrentIndex = _optimumEndIndex = 0;
            GetMatches(inStream);
            UInt32 lenEnd;
            {
                var numDistancePairs = _matchDistances[0];
                if (numDistancePairs == 0)
                    return (0, 1);
                var matchDistances = _matchDistances + 1;
                lenEnd = matchDistances[numDistancePairs - 2];

                if (lenEnd > _numFastBytes)
                {
                    var backRes = matchDistances[numDistancePairs - 1];
                    MovePos(inStream, lenEnd - 1);
                    return (backRes, lenEnd);
                }
                _optimum[1].Price = _literalPrices[(_lzInWindow.CurrentPos - _additionalOffset)[0]];
                _optimum[1].PosPrev = 0;
                _optimum[2].Price = DeflateEncoderConstants.kIfinityPrice;
                _optimum[2].PosPrev = 1;
                var offs = 0;
                for (var i = DeflateConstants.kMatchMinLen; i <= lenEnd; i++)
                {
                    var distance = matchDistances[offs + 1];
                    _optimum[i].PosPrev = 0;
                    _optimum[i].BackPrev = distance;
                    _optimum[i].Price = (UInt32)_lenPrices[i - DeflateConstants.kMatchMinLen] + _posPrices[GetPosSlot(distance)];
                    if (i == matchDistances[offs])
                        offs += 2;
                }
            }
            var cur = 0U;
            while (true)
            {
                ++cur;
                if (cur == lenEnd || cur == DeflateEncoderConstants.kNumOptsBase || _pos >= DeflateEncoderConstants.kMatchArrayLimit)
                    return Backward(cur);
                GetMatches(inStream);
                var matchDistances = _matchDistances + 1;
                var numDistancePairs = _matchDistances[0];
                var newLen = 0U;
                if (numDistancePairs != 0)
                {
                    newLen = matchDistances[numDistancePairs - 2];
                    if (newLen > _numFastBytes)
                    {
                        var (backRes, len) = Backward(cur);
                        _optimum[cur].BackPrev = matchDistances[numDistancePairs - 1];
                        _optimumEndIndex = cur + newLen;
                        _optimum[cur].PosPrev = (UInt16)_optimumEndIndex;
                        MovePos(inStream, newLen - 1);
                        return (backRes, len);
                    }
                }
                var curPrice = _optimum[cur].Price;
                {
                    var curAnd1Price = curPrice + _literalPrices[(_lzInWindow.CurrentPos + cur - _additionalOffset)[0]];
                    var optimum = _optimum[(Int32)cur + 1];
                    if (curAnd1Price < optimum.Price)
                    {
                        optimum.Price = curAnd1Price;
                        optimum.PosPrev = (UInt16)cur;
                    }
                }
                if (numDistancePairs != 0)
                {
                    while (lenEnd < cur + newLen)
                        _optimum[++lenEnd].Price = DeflateEncoderConstants.kIfinityPrice;
                    var offs = 0;
                    var distance = matchDistances[offs + 1];
                    curPrice += _posPrices[GetPosSlot(distance)];
                    for (var lenTest = DeflateConstants.kMatchMinLen; ; lenTest++)
                    {
                        var curAndLenPrice = curPrice + _lenPrices[lenTest - DeflateConstants.kMatchMinLen];
                        var optimum = _optimum[cur + lenTest];
                        if (curAndLenPrice < optimum.Price)
                        {
                            optimum.Price = curAndLenPrice;
                            optimum.PosPrev = (UInt16)cur;
                            optimum.BackPrev = distance;
                        }
                        if (lenTest == matchDistances[offs])
                        {
                            offs += 2;
                            if (offs == numDistancePairs)
                                break;
                            curPrice -= _posPrices[GetPosSlot(distance)];
                            distance = matchDistances[offs + 1];
                            curPrice += _posPrices[GetPosSlot(distance)];
                        }
                    }
                }
            }
        }

        private (UInt32 Pos, UInt32 Len) Backward(UInt32 cur)
        {
            _optimumEndIndex = cur;
            var posMem = _optimum[cur].PosPrev;
            var backMem = _optimum[cur].BackPrev;
            do
            {
                var posPrev = posMem;
                var backCur = backMem;
                backMem = _optimum[posPrev].BackPrev;
                posMem = _optimum[posPrev].PosPrev;
                _optimum[posPrev].BackPrev = backCur;
                _optimum[posPrev].PosPrev = (UInt16)cur;
                cur = posPrev;
            }
            while (cur > 0);
            return (_optimum[0].BackPrev, _optimum[0].PosPrev);
        }

        private UInt32 GetLzBlockPrice()
        {
            return
              Huffman_GetPrice_Spec(
                  _mainFreqs,
                  _newLevels.LitLenLevels,
                  DeflateConstants.kFixedMainTableSize,
                  _lenDirectBits.Span,
                  DeflateConstants.kSymbolMatch)
              + Huffman_GetPrice_Spec(
                  _distFreqs,
                  _newLevels.DistLevels,
                  DeflateConstants.kDistTableSize64,
                  DeflateConstants.kDistDirectBits.Span,
                  0);
        }

        private static UInt32 Huffman_GetPrice_Spec(ReadOnlySpan<UInt32> freqs, ReadOnlySpan<Byte> lens, UInt32 num, ReadOnlySpan<Byte> extraBits, UInt32 extraBase)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            return
                Huffman_GetPrice(freqs, lens, num)
                + Huffman_GetPrice(freqs.Slice(extraBase), extraBits, num - extraBase);
        }

        private static UInt32 Huffman_GetPrice(ReadOnlySpan<UInt32> freqs, ReadOnlySpan<Byte> lens, UInt32 num)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            var price = 0U;
            for (var i = 0; i < num; i++)
                price += lens[i] * freqs[i];
            return price;
        }

        private static UInt32 GetStorePrice(UInt32 blockSize, UInt32 bitPosition)
        {
            var price = 0U;
            do
            {
                var nextBitPosition = (bitPosition + DeflateConstants.kFinalBlockFieldSize + DeflateConstants.kBlockTypeFieldSize) & 7;
                var numBitsForAlign = nextBitPosition > 0 ? (8 - nextBitPosition) : 0;
                var curBlockSize = blockSize.Maximum(UInt16.MaxValue);
                price += DeflateConstants.kFinalBlockFieldSize + DeflateConstants.kBlockTypeFieldSize + numBitsForAlign + (2 + 2) * 8 + curBlockSize * 8;
                bitPosition = 0;
                blockSize -= curBlockSize;
            }
            while (blockSize > 0);
            return price;
        }

        private void SetPrices(Levels levels)
        {
            if (_fastMode)
                return;
            var lenSlots = DeflateEncoderConstants.LenSlots.Span;
            var lenDirectBits = _lenDirectBits.Span;
            var kDistDirectBits = DeflateConstants.kDistDirectBits.Span;
            for (var i = 0; i < 256; i++)
            {
                var price = levels.LitLenLevels[i];
                _literalPrices[i] = (price != 0) ? price : DeflateEncoderConstants.kNoLiteralStatPrice;
            }
            for (var i = 0; i < _numLenCombinations; i++)
            {
                var slot = lenSlots[i];
                var price = levels.LitLenLevels[DeflateConstants.kSymbolMatch + slot];
                _lenPrices[i] = (Byte)(((price != 0) ? price : DeflateEncoderConstants.kNoLenStatPrice) + lenDirectBits[slot]);
            }
            for (var i = 0; i < DeflateConstants.kDistTableSize64; i++)
            {
                var price = levels.DistLevels[i];
                _posPrices[i] = (Byte)(((price != 0) ? price : DeflateEncoderConstants.kNoPosStatPrice) + kDistDirectBits[i]);
            }
        }

        private void MakeTables(UInt32 maxHuffLen)
        {
            _mainFreqs.AsReadOnlySpan().HuffmanGenerate(_mainCodes, _newLevels.LitLenLevels, DeflateConstants.kFixedMainTableSize, maxHuffLen);
            _distFreqs.AsReadOnlySpan().HuffmanGenerate(_distCodes, _newLevels.DistLevels, DeflateConstants.kDistTableSize64, maxHuffLen);
        }

        private static void LevelTableDummy(ReadOnlySpan<Byte> levels, UInt32 numLevels, Span<UInt32> freqs)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            var prevLen = (Int32)Byte.MaxValue;
            var nextLen = (Int32)levels[0];
            var count = 0U;
            var maxCount = 7U;
            var minCount = 4U;
            if (nextLen == 0)
            {
                maxCount = 138;
                minCount = 3;
            }
            for (var n = 0U; n < numLevels; n++)
            {
                var curLen = nextLen;
                nextLen = (n < numLevels - 1) ? levels[(Int32)n + 1] : Byte.MaxValue;
                count++;
                if (count >= maxCount || curLen != nextLen)
                {
                    if (count < minCount)
                        freqs[curLen] += count;
                    else if (curLen != 0)
                    {
                        if (curLen != prevLen)
                            freqs[curLen]++;
                        freqs[DeflateConstants.kTableLevelRepNumber]++;
                    }
                    else if (count <= 10)
                        freqs[DeflateConstants.kTableLevel0Number]++;
                    else
                        freqs[DeflateConstants.kTableLevel0Number2]++;
                    count = 0;
                    prevLen = curLen;
                    if (nextLen == 0)
                    {
                        maxCount = 138;
                        minCount = 3;
                    }
                    else if (curLen == nextLen)
                    {
                        maxCount = 6;
                        minCount = 3;
                    }
                    else
                    {
                        maxCount = 7;
                        minCount = 4;
                    }
                }
            }
        }

        private void LevelTableCode(IBasicOutputByteStream outStream, ReadOnlySpan<Byte> levels, UInt32 numLevels, ReadOnlySpan<Byte> lens, ReadOnlySpan<UInt32> codes)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            var prevLen = (Int32)Byte.MaxValue;
            var nextLen = (Int32)levels[0];
            var count = 0U;
            var maxCount = 7U;
            var minCount = 4U;
            if (nextLen == 0)
            {
                maxCount = 138;
                minCount = 3;
            }
            for (var n = 0; n < numLevels; n++)
            {
                var curLen = nextLen;
                nextLen = (n < numLevels - 1) ? levels[n + 1] : Byte.MaxValue;
                ++count;
                if (count >= maxCount || curLen != nextLen)
                {
                    if (count < minCount)
                    {
                        for (var i = 0; i < count; i++)
                            WriteBits(outStream, codes[curLen], lens[curLen]);
                    }
                    else if (curLen != 0)
                    {
                        if (curLen != prevLen)
                        {
                            WriteBits(outStream, codes[curLen], lens[curLen]);
                            --count;
                        }
                        WriteBits(outStream, codes[DeflateConstants.kTableLevelRepNumber], lens[DeflateConstants.kTableLevelRepNumber]);
                        WriteBits(outStream, count - 3, 2);
                    }
                    else if (count <= 10)
                    {
                        WriteBits(outStream, codes[DeflateConstants.kTableLevel0Number], lens[DeflateConstants.kTableLevel0Number]);
                        WriteBits(outStream, count - 3, 3);
                    }
                    else
                    {
                        WriteBits(outStream, codes[DeflateConstants.kTableLevel0Number2], lens[DeflateConstants.kTableLevel0Number2]);
                        WriteBits(outStream, count - 11, 7);
                    }
                    count = 0;
                    prevLen = curLen;
                    if (nextLen == 0)
                    {
                        maxCount = 138;
                        minCount = 3;
                    }
                    else if (curLen == nextLen)
                    {
                        maxCount = 6;
                        minCount = 3;
                    }
                    else
                    {
                        maxCount = 7;
                        minCount = 4;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static UInt32 GetPosSlot(UInt32 pos)
        {
            var zz = (DeflateEncoderConstants.kNumLogBits - 1)
               & (((1U << DeflateEncoderConstants.kNumLogBits) - 1 - pos) >> (31 - 3));
            return DeflateEncoderConstants.FastPos.Span[(Int32)(pos >> (Int32)zz)] + (zz * 2);
        }

        private void GetMatches(IBasicInputByteStream inStream)
        {
            if (_isMultiPass)
            {
                _matchDistances = _onePosMatchesMemory.GetPointer(_pos);
                if (_secondPass)
                {
                    _pos += (UInt32)_matchDistances[0] + 1;
                    return;
                }
            }
            Span<UInt32> distanceTmp = stackalloc UInt32[DeflateConstants.kMatchMaxLen * 2 + 3];
            var numPairs = _lzInWindow.GetMatches(inStream, distanceTmp);
            _matchDistances[0] = (UInt16)numPairs;
            if (numPairs != 0)
            {
                var i = 0;
                while (i < numPairs)
                {
                    _matchDistances[i + 1] = (UInt16)distanceTmp[i];
                    _matchDistances[i + 2] = (UInt16)distanceTmp[i + 1];
                    i += 2;
                }
                var len = distanceTmp[(Int32)numPairs - 2];
                if (len == _numFastBytes && _numFastBytes != _matchMaxLen)
                {
                    var numAvail = (_lzInWindow.NumAvailableBytes + 1).Minimum(_matchMaxLen);
                    var pby = _lzInWindow.CurrentPos - 1;
                    var pby2 = pby - (distanceTmp[(Int32)numPairs - 1] + 1);
                    while (len < numAvail && pby[len] == pby2[len])
                        ++len;
                    _matchDistances[i - 1] = (UInt16)len;
                }
            }
            if (_isMultiPass)
                _pos += numPairs + 1;
            if (!_secondPass)
                ++_additionalOffset;
        }

        private void MovePos(IBasicInputByteStream inStream, UInt32 num)
        {
            if (!_secondPass && num > 0)
            {
                _lzInWindow.Skip(inStream, num);
                _additionalOffset += num;
            }
        }

        private void WriteBlock(IBasicOutputByteStream outStream)
        {
            Huffman_ReverseBits(_mainCodes, _newLevels.LitLenLevels, DeflateConstants.kFixedMainTableSize);
            Huffman_ReverseBits(_distCodes, _newLevels.DistLevels, DeflateConstants.kDistTableSize64);

            var lenSlots = DeflateEncoderConstants.LenSlots.Span;
            var lenStart = _lenStart.Span;
            var kDistStart = DeflateConstants.kDistStart.Span;
            var kDistDirectBits = DeflateConstants.kDistDirectBits.Span;
            for (UInt32 i = 0; i < _valueIndex; i++)
            {
                var codeValue = _values[i];
                if (codeValue.IsLiteral)
                    this.outStream.WriteBits(outStream, _mainCodes[codeValue.Pos], _newLevels.LitLenLevels[codeValue.Pos]);
                else
                {
                    var len = codeValue.Len;
                    var lenSlot = (Int32)lenSlots[len];
                    this.outStream.WriteBits(outStream, _mainCodes[DeflateConstants.kSymbolMatch + lenSlot], _newLevels.LitLenLevels[DeflateConstants.kSymbolMatch + lenSlot]);
                    this.outStream.WriteBits(outStream, (UInt32)len - lenStart[lenSlot], _lenDirectBits.Span[lenSlot]);
                    UInt32 dist = codeValue.Pos;
                    UInt32 posSlot = GetPosSlot(dist);
                    this.outStream.WriteBits(outStream, _distCodes[posSlot], _newLevels.DistLevels[posSlot]);
                    this.outStream.WriteBits(outStream, dist - kDistStart[(Int32)posSlot], kDistDirectBits[(Int32)posSlot]);
                }
            }
            this.outStream.WriteBits(outStream, _mainCodes[DeflateConstants.kSymbolEndOfBlock], _newLevels.LitLenLevels[DeflateConstants.kSymbolEndOfBlock]);
        }

        private void WriteStoreBlock(IBasicOutputByteStream outStream, UInt32 blockSize, UInt32 additionalOffset, bool finalBlock)
        {
            do
            {
                var curBlockSize = blockSize.Maximum(UInt16.MaxValue);
                blockSize -= curBlockSize;
                WriteBits(outStream, finalBlock && (blockSize == 0) ? DeflateConstants.FinalBlockField.kFinalBlock : DeflateConstants.FinalBlockField.kNotFinalBlock, DeflateConstants.kFinalBlockFieldSize);
                WriteBits(outStream, DeflateConstants.BlockType.kStored, DeflateConstants.kBlockTypeFieldSize);
                this.outStream.FlushByte(outStream);
                WriteBits(outStream, (UInt16)curBlockSize, DeflateConstants.kStoredBlockLengthFieldSize);
                WriteBits(outStream, (UInt16)~curBlockSize, DeflateConstants.kStoredBlockLengthFieldSize);
                ReadOnlyArrayPointer<Byte> data = _lzInWindow.CurrentPos - additionalOffset;
                for (var i = 0; i < curBlockSize; i++)
                    this.outStream.WriteByte(outStream, data[i]);
                additionalOffset -= curBlockSize;
            }
            while (blockSize != 0);
        }

        private void WriteBits(IBasicOutputByteStream outStream, UInt32 value, Int32 numBits)
        {
            this.outStream.WriteBits(outStream, value, numBits);
        }

        private static void Huffman_ReverseBits(Span<UInt32> codes, ReadOnlySpan<Byte> lens, UInt32 num)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            for (var i = 0; (UInt32)i < num; i++)
            {
                var x = codes[i];
                x = ((x & 0x5555) << 1) | ((x & 0xAAAA) >> 1);
                x = ((x & 0x3333) << 2) | ((x & 0xCCCC) >> 2);
                x = ((x & 0x0F0F) << 4) | ((x & 0xF0F0) >> 4);
                codes[i] = (((x & Byte.MaxValue) << 8) | ((x & 0xFF00) >> 8)) >> (16 - lens[i]);
            }
        }
    }
}
