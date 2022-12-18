namespace SevenZip.Compression.Lzma.Encoder
{
    public enum MatchFinderType
    {
        BT2,
        // BT3, // not used as only BT2 and BT4 are supported
        BT4,
        // BT5, // not used as only BT2 and BT4 are supported
        // HC4, // not used as only BT2 and BT4 are supported
        // HC5, // not used as only BT2 and BT4 are supported
    };
}
