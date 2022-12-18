namespace ZipUtility.IO.Compression
{
    public class LzmaCompressionOption
        : ICoderOption
    {
        public bool UseEndOfStreamMarker { get; set; }
    }
}
