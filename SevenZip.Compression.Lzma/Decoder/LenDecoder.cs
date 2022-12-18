// based on LZMA SDK 19.00 (LZMA SDK is written and placed in the public domain by Igor Pavlov.)

using SevenZip.Compression.Lzma.Decoder.RangeDecoder;
using System;
using Utility;
using Utility.IO;

namespace SevenZip.Compression.Lzma.Decoder
{
    class LenDecoder
    {
        private readonly BitDecoder _choice;
        private readonly BitDecoder _choice2;
        private readonly BitTreeDecoder[] _lowCoder;
        private readonly BitTreeDecoder[] _midCoder;
        private readonly BitTreeDecoder _highCoder;

        public LenDecoder(UInt32 numPosStates)
        {
            if (numPosStates > LzmaConstants.kNumPosStatesMax)
                throw new ArgumentOutOfRangeException(nameof(numPosStates));

            _choice = new BitDecoder();
            _choice2 = new BitDecoder();
            _lowCoder = new BitTreeDecoder[numPosStates];
            _lowCoder.FillArray(_ => new BitTreeDecoder(LzmaConstants.kNumLowLenBits));
            _midCoder = new BitTreeDecoder[numPosStates];
            _midCoder.FillArray(_ => new BitTreeDecoder(LzmaConstants.kNumMidLenBits));
            _highCoder = new BitTreeDecoder(LzmaConstants.kNumHighLenBits);
        }

        public UInt32 Decode(IBasicInputByteStream inStream, RangeDecoder.Decoder rangeDecoder, UInt32 posState)
        {
            if (_choice.Decode(inStream, rangeDecoder) == 0)
                return _lowCoder[posState].Decode(inStream, rangeDecoder);
            else if (_choice2.Decode(inStream, rangeDecoder) == 0)
            {
                return
                    LzmaConstants.kNumLowLenSymbols
                    + _midCoder[posState].Decode(inStream, rangeDecoder);
            }
            else
            {
                return
                    LzmaConstants.kNumLowLenSymbols
                    + LzmaConstants.kNumMidLenSymbols
                    + _highCoder.Decode(inStream, rangeDecoder);
            }
        }
    }
}
