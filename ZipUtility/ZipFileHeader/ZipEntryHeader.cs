namespace ZipUtility.ZipFileHeader
{
    class ZipEntryHeader
    {
        public ZipEntryHeader(ZipEntryCentralDirectoryHeader centralDirectoryHeader, ZipEntryLocaFilelHeader localFileHeader)
        {
            CentralDirectoryHeader = centralDirectoryHeader;
            LocalFileHeader = localFileHeader;
        }

        public ZipEntryCentralDirectoryHeader CentralDirectoryHeader { get; }
        public ZipEntryLocaFilelHeader LocalFileHeader { get; }
    }
}