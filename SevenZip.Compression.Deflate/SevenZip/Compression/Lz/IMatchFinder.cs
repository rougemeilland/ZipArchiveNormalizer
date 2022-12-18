// based on 7-Zip 21.03 Copyright (C) 1999-2021 Igor Pavlov.

using System;
using Utility;
using Utility.IO;

namespace SevenZip.Compression.Lz
{
    interface IMatchFinder
    {
        void Initialize(IBasicInputByteStream inStream);
        UInt32 BlockSize { get; }
        UInt32 GetMatchLen(Int32 index, UInt32 distance, UInt32 limit);
#if true // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
        ReadOnlyArrayPointer<Byte> CurrentPos { get; }
#else
        ref Byte CurrentPos { get; }
#endif
        UInt32 NumAvailableBytes { get; }
        UInt32 GetMatches(IBasicInputByteStream inStream, Span<UInt32> distances); // TODO: 正常動作を確認後、SpanやMemoryをポインタに置き換えることを考慮に入れて高速化を試みる。
        void Skip(IBasicInputByteStream inStream, UInt32 num);
    }
}
