namespace ZipUtility.IO.Compression.Stored
{
    public abstract class StoredCoderPlugin
        : ICompressionCoder
    {
        private class DummyOption
            : ICoderOption
        {
        }

        public CompressionMethodId CompressionMethodId => CompressionMethodId.Stored;

        public ICoderOption DefaultOption => new DummyOption();

        public ICoderOption GetOptionFromGeneralPurposeFlag(bool bit1, bool bit2) => new DummyOption();
    }
}
