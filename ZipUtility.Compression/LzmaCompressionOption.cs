namespace ZipUtility.Compression
{
    public class LzmaCompressionOption
        : ICompressionOption
    {
        public bool UseEndOfStreamMarker { get; set; }
    }
}
