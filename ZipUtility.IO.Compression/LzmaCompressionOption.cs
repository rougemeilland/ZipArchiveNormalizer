namespace ZipUtility.IO.Compression
{
    public class LzmaCompressionOption
        : ICompressionOption
    {
        public bool UseEndOfStreamMarker { get; set; }
    }
}
