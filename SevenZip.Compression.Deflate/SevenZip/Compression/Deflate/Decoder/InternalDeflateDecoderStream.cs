// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

//#define ENABLE_NSIS_MODE_SPECIFICATION
//#define ENABLE_FINISH_MODE_SPECIFICATION
using System;
using System.Threading;
using System.Threading.Tasks;
using Utility;
using Utility.IO;

namespace SevenZip.Compression.Deflate.Decoder
{
    class InternalDeflateDecoderStream
        : IBasicInputByteStream
    {
        private readonly IBasicInputByteStream _baseStream;
        private readonly IProgress<UInt64>? _progress;
        private readonly bool _leaveOpen;
        private readonly Lz.LzOutWindow _outWindowStream;
        private readonly BitLsb.Decoder<BitLsb.InBuffer> _inBitStream;
        private readonly Huffman.Decoder _mainDecoder;
        private readonly Huffman.Decoder _distDecoder;
        private readonly Huffman.Decoder7b _levelDecoder;
        private readonly UInt64? _outSize;
#if ENABLE_NSIS_MODE_SPECIFICATION
        private readonly bool _deflateNSIS;
#else
        private const bool _deflateNSIS = false;
#endif
        private readonly bool _deflate64Mode;
#if ENABLE_FINISH_MODE_SPECIFICATION
        private readonly bool _needFinishInput;
#else
        private const bool _needFinishInput = false;
#endif

        private bool _isDisposed;
        private UInt32 _storedBlockSize;
        private UInt32 _numDistLevels;
        private bool _finalBlock;
        private bool _storedMode;
        private bool _needReadTable;
        private Int32 _remainLen;
        private UInt32 _rep0;
        private UInt64 _processedCount;

        public InternalDeflateDecoderStream(bool deflate64Mode, IBasicInputByteStream baseStream, UInt64? unpackedStreamSize, IProgress<UInt64>? progress, bool leaveOpen)
        {
            try
            {
                if (baseStream is null)
                    throw new ArgumentNullException(nameof(baseStream));

                _isDisposed = false;
                _baseStream = baseStream;
                _progress = progress;
                _leaveOpen = leaveOpen;
                _outWindowStream = new Lz.LzOutWindow(deflate64Mode ? DeflateConstants.kHistorySize64 : DeflateConstants.kHistorySize32);
                _inBitStream = new BitLsb.Decoder<BitLsb.InBuffer>(new BitLsb.InBuffer(1 << 20));
                _mainDecoder = new Huffman.Decoder(DeflateConstants.kNumHuffmanBits, DeflateConstants.kFixedMainTableSize);
                _distDecoder = new Huffman.Decoder(DeflateConstants.kNumHuffmanBits, DeflateConstants.kFixedDistTableSize);
                _levelDecoder = new Huffman.Decoder7b(DeflateConstants.kLevelTableSize);
                _storedBlockSize = 0;
                _numDistLevels = 0;
                _finalBlock = false;
                _storedMode = false;
#if ENABLE_NSIS_MODE_SPECIFICATION
                _deflateNSIS = properties.NsisMode;
#endif
                _deflate64Mode = deflate64Mode;
#if ENABLE_FINISH_MODE_SPECIFICATION
                _needFinishInput = properties.NeedFinishInput;
#endif
                _needReadTable = true;
                _remainLen = 0;
                _rep0 = 0;
                _outSize = unpackedStreamSize;
                _processedCount = 0;
            }
            catch (Exception)
            {
                if (!leaveOpen)
                    baseStream?.Dispose();
                throw;
            }
        }

        public Int32 Read(Span<byte> buffer)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            return InternalRead(buffer);
        }

        public Task<Int32> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            return Task.FromResult(InternalRead(buffer.Span));
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
                if (!_leaveOpen)
                    await _baseStream.DisposeAsync().ConfigureAwait(false);
                _isDisposed = true;
            }
        }

        private Int32 InternalRead(Span<byte> buffer)
        {
            var size = (UInt32)buffer.Length;
#if ENABLE_FINISH_MODE_SPECIFICATION
            bool finishInputStream = false;
#else
            const bool finishInputStream = false;
#endif
            if (_outSize.HasValue)
            {
                if (_outWindowStream.ProcessedSize > _outSize.Value)
                    throw new InternalLogicalErrorException();
                var rem = _outSize.Value - _outWindowStream.ProcessedSize;
                if (size >= rem)
                {
                    size = (UInt32)rem;
#if ENABLE_FINISH_MODE_SPECIFICATION
                    if (ZlibMode || _needFinishInput)
                        finishInputStream = true;
#endif
                }
            }
            if (!finishInputStream && size <= 0)
                return 0;
            Int32 bufferIndex = 0;
#if ENABLE_FINISH_MODE_SPECIFICATION
            CodeSpec(_baseStream, buffer, ref bufferIndex, size, finishInputStream);
#else
            CodeSpec(_baseStream, buffer, ref bufferIndex, size);
#endif
            _outWindowStream.Flush(buffer, ref bufferIndex);
            _processedCount += (UInt32)bufferIndex;
            try
            {
                _progress?.Report(_processedCount);
            }
            catch (Exception)
            {
            }
            return bufferIndex;
        }

#if ENABLE_FINISH_MODE_SPECIFICATION
        private void CodeSpec(IBasicInputByteStream inStream, Span<Byte> buffer, ref Int32 bufferIndex, UInt32 curSize, bool finishInputStream, UInt32 inputProgressLimit = 0)
        {
#else
        private void CodeSpec(IBasicInputByteStream inStream, Span<Byte> buffer, ref Int32 bufferIndex, UInt32 curSize, UInt32 inputProgressLimit = 0)
        {
            const bool finishInputStream = false;
#endif
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            var kLenStart = _deflate64Mode ? DeflateConstants.kLenStart64.Span : DeflateConstants.kLenStart32.Span;
            var kLenDirectBits = _deflate64Mode ? DeflateConstants.kLenDirectBits64.Span : DeflateConstants.kLenDirectBits32.Span;
            var kDistStart = DeflateConstants.kDistStart.Span;
            var kDistDirectBits = DeflateConstants.kDistDirectBits.Span;
            if (_remainLen == DeflateDecoderConstants.kLenIdFinished)
                return;
            while (_remainLen > 0 && curSize > 0)
            {
                --_remainLen;
                var b = _outWindowStream.GetByte(_rep0);
                _outWindowStream.PutByte(buffer, ref bufferIndex, b);
                --curSize;
            }
            var inputStart = inputProgressLimit > 0 ? _inBitStream.ProcessedSize : 0UL;
            while (curSize > 0 || finishInputStream)
            {
                if (_inBitStream.ExtraBitsWereRead)
                    throw new SevenZipDataErrorException();
                if (_needReadTable)
                {
                    if (_finalBlock)
                    {
                        _remainLen = DeflateDecoderConstants.kLenIdFinished;
                        break;
                    }
                    if (inputProgressLimit > 0)
                        if (_inBitStream.ProcessedSize - inputStart >= inputProgressLimit)
                            return;
                    ReadTables(inStream);
                    if (_inBitStream.ExtraBitsWereRead)
                        throw new SevenZipDataErrorException();
                    _needReadTable = false;
                }
                if (_storedMode)
                {
                    if (finishInputStream && curSize <= 0 && _storedBlockSize > 0)
                        throw new SevenZipDataErrorException();
                    while (_storedBlockSize > 0 && curSize > 0 && _inBitStream.ThereAreDataInBitsBuffer)
                    {
                        _outWindowStream.PutByte(buffer, ref bufferIndex, ReadAlignedByte(inStream));
                        --_storedBlockSize;
                        --curSize;
                    }
                    while (_storedBlockSize > 0 && curSize > 0)
                    {
                        _outWindowStream.PutByte(buffer, ref bufferIndex, _inBitStream.ReadDirectByte(inStream));
                        --_storedBlockSize;
                        --curSize;
                    }
                    _needReadTable = _storedBlockSize == 0;
                }
                else
                {
                    while (curSize > 0)
                    {
                        if (_inBitStream.ExtraBitsWereReadFast)
                            throw new SevenZipDataErrorException();
                        var sym = _mainDecoder.Decode(inStream, _inBitStream);
                        if (sym < 0x100)
                        {
                            _outWindowStream.PutByte(buffer, ref bufferIndex, (Byte)sym);
                            --curSize;
                        }
                        else if (sym == DeflateConstants.kSymbolEndOfBlock)
                        {
                            _needReadTable = true;
                            break;
                        }
                        else if (sym < DeflateConstants.kMainTableSize)
                        {
                            sym -= DeflateConstants.kSymbolMatch;
                            var len =
                                 kLenStart[(Int32)sym]
                                 + _inBitStream.ReadBits(inStream, kLenDirectBits[(Int32)sym])
                                 + DeflateConstants.kMatchMinLen;
                            var locLen = len.Minimum(curSize);
                            sym = _distDecoder.Decode(inStream, _inBitStream);
                            if (sym >= _numDistLevels)
                                throw new SevenZipDataErrorException();
                            sym =
                                kDistStart[(Int32)sym]
                                + _inBitStream.ReadBits(inStream, kDistDirectBits[(Int32)sym]);
                            if (!_outWindowStream.CopyBlock(buffer, ref bufferIndex, sym, locLen))
                                throw new SevenZipDataErrorException();
                            curSize -= locLen;
                            len -= locLen;
                            if (len != 0)
                            {
                                _remainLen = (Int32)len;
                                _rep0 = sym;
                                break;
                            }
                        }
                        else
                            throw new SevenZipDataErrorException();
                    }
                    if (finishInputStream && curSize == 0)
                    {
                        if (_mainDecoder.Decode(inStream, _inBitStream) != DeflateConstants.kSymbolEndOfBlock)
                            throw new SevenZipDataErrorException();
                        _needReadTable = true;
                    }
                }
            }
            if (_inBitStream.ExtraBitsWereRead)
                throw new SevenZipDataErrorException();
            return;
        }

        private void ReadTables(IBasicInputByteStream inStream)
        {
            _finalBlock = ReadBits(inStream, DeflateConstants.kFinalBlockFieldSize) == DeflateConstants.FinalBlockField.kFinalBlock;
            if (_inBitStream.ExtraBitsWereRead)
                throw new SevenZipDataErrorException();
            var blockType = ReadBits(inStream, DeflateConstants.kBlockTypeFieldSize);
            if (blockType > DeflateConstants.BlockType.kDynamicHuffman)
                throw new SevenZipDataErrorException();
            if (_inBitStream.ExtraBitsWereRead)
                throw new SevenZipDataErrorException();
            if (blockType == DeflateConstants.BlockType.kStored)
            {
                _storedMode = true;
                _inBitStream.AlignToByte();
                _storedBlockSize = ReadAlignedUInt16(inStream);
#if ENABLE_NSIS_MODE_SPECIFICATION
                if (_deflateNSIS)
                    return;
#endif
                if (_storedBlockSize != (UInt16)~ReadAlignedUInt16(inStream))
                    throw new SevenZipDataErrorException();
                return;
            }
            _storedMode = false;
            var levels = new Levels();
            if (blockType == DeflateConstants.BlockType.kFixedHuffman)
            {
                levels.SetFixedLevels();
                _numDistLevels = _deflate64Mode ? DeflateConstants.kDistTableSize64 : (UInt32)DeflateConstants.kDistTableSize32;
            }
            else
            {
                var numLitLenLevels = ReadBits(inStream, DeflateConstants.kNumLenCodesFieldSize) + DeflateConstants.kNumLitLenCodesMin;
                _numDistLevels = ReadBits(inStream, DeflateConstants.kNumDistCodesFieldSize) + DeflateConstants.kNumDistCodesMin;
                var numLevelCodes = ReadBits(inStream, DeflateConstants.kNumLevelCodesFieldSize) + DeflateConstants.kNumLevelCodesMin;
                if (!_deflate64Mode && _numDistLevels > DeflateConstants.kDistTableSize32)
                    throw new SevenZipDataErrorException();
                var kCodeLengthAlphabetOrder = DeflateConstants.kCodeLengthAlphabetOrder.Span;
                Span<Byte> levelLevels = stackalloc Byte[DeflateConstants.kLevelTableSize];
                for (var i = 0; i < DeflateConstants.kLevelTableSize; i++)
                {
                    var position = kCodeLengthAlphabetOrder[i];
                    if (i < numLevelCodes)
                        levelLevels[position] = (Byte)ReadBits(inStream, DeflateConstants.kLevelFieldSize);
                    else
                        levelLevels[position] = 0;
                }
                if (_inBitStream.ExtraBitsWereRead)
                    throw new SevenZipDataErrorException();
                if (!_levelDecoder.Build(levelLevels))
                    throw new SevenZipDataErrorException();
                Span<Byte> tmpLevels = stackalloc Byte[DeflateConstants.kFixedMainTableSize + DeflateConstants.kFixedDistTableSize];
                DecodeLevels(inStream, tmpLevels, numLitLenLevels + _numDistLevels);
                if (_inBitStream.ExtraBitsWereRead)
                    throw new SevenZipDataErrorException();
                levels.SubClear();
                levels.SetLitLenLevels(tmpLevels.Slice(0, numLitLenLevels));
                levels.SetDistLevels(tmpLevels.Slice(numLitLenLevels, _numDistLevels));
            }
            if (!_mainDecoder.Build(levels.LitLenLevels))
                throw new SevenZipDataErrorException();
            if (!_distDecoder.Build(levels.DistLevels))
                throw new SevenZipDataErrorException();
        }

        private void DecodeLevels(IBasicInputByteStream inStream, Span<Byte> levels, UInt32 numSymbols)
        {
            // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
            var i = 0U;
            do
            {
                var sym = _levelDecoder.Decode(inStream, _inBitStream);
                if (sym < DeflateConstants.kTableDirectLevels)
                    levels[(Int32)i++] = (Byte)sym;
                else
                {
                    if (sym >= DeflateConstants.kLevelTableSize)
                        throw new SevenZipDataErrorException();
                    UInt32 num;
                    Int32 numBits;
                    Byte symbol;
                    if (sym == DeflateConstants.kTableLevelRepNumber)
                    {
                        if (i == 0)
                            throw new SevenZipDataErrorException();
                        numBits = 2;
                        num = 0;
                        symbol = levels[(Int32)i - 1];
                    }
                    else
                    {
                        sym -= DeflateConstants.kTableLevel0Number;
                        sym <<= 2;
                        numBits = (Int32)(3 + sym);
                        num = sym << 1;
                        symbol = 0;
                    }
                    num += 3 + ReadBits(inStream, numBits);
                    if (num + i > numSymbols)
                        throw new SevenZipDataErrorException();
                    levels.Slice(i, num).FillArray(symbol);
                    i += num;
                }
            }
            while (i < numSymbols);
        }

        private UInt32 ReadBits(IBasicInputByteStream inStream, Int32 numBits)
        {
            return _inBitStream.ReadBits(inStream, numBits);
        }

        private Byte ReadAlignedByte(IBasicInputByteStream inStream)
        {
            return _inBitStream.ReadAlignedByte(inStream);
        }

        private UInt32 ReadAlignedUInt16(IBasicInputByteStream inStream)
        {
            var low = _inBitStream.ReadAlignedByte(inStream);
            var high = _inBitStream.ReadAlignedByte(inStream);
            return ((UInt32)high << 8) | low;
        }
    }
}
