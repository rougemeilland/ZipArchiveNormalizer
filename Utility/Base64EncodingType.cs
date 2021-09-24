namespace Utility
{
    public enum Base64EncodingType
    {
        Default = 0,
        Rrc4648Encoding = 0,

        MimeEncoding = 1,
        Rrc2045Encoding = 1,

        Radix64Encoding = 2,
        Rrc4880Encoding = 2,
    }
}
