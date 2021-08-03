using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using ZipArchiveNormalizer.ZipExtraField;

namespace ZipArchiveNormalizer
{
    class ZipArchiveEntry
    {
#if true
        private static Encoding _shiftJisEncoding;
#else
        private static Regex _keyValuePattern;
#endif

        static ZipArchiveEntry()
        {
#if true
            _shiftJisEncoding = Encoding.GetEncoding("shift_jis");
#else
            _keyValuePattern = new Regex(@"^(?<key>[^=]+) = (?<value>.*)$");
#endif
        }

#if true
        public ZipArchiveEntry(ZipEntry zipEntry)
        {
            IsDirectory = zipEntry.IsDirectory;
            FullName = zipEntry.Name;
            Comment = zipEntry.Comment;
            Offset = zipEntry.Offset;
            CRC = zipEntry.Crc;
            Size = zipEntry.Size;
            PackedSize = zipEntry.CompressedSize;
            HostSystem = zipEntry.HostSystem;
            ExternalFileAttributes = zipEntry.ExternalFileAttributes;
            EntryTextEncoding = zipEntry.IsUnicodeText ? ZipArchiveEntryTextEncoding.UTF8 : ZipArchiveEntryTextEncoding.Local;
            ExtraData = zipEntry.ExtraData?.ToArray();
            using (var extraData = new ZipExtraData(zipEntry.ExtraData))
            {
                UnixExtraData1 = extraData.GetData<ExtendedUnixData>();
                UnixExtraData2 = extraData.GetData<UnixExtraField>();
                WindowsExtraData = extraData.GetData<NTTaggedData>();
                if (UnixExtraData1 != null && (UnixExtraData1.Include & ExtendedUnixData.Flags.ModificationTime) != 0)
                    LastModificationTime = UnixExtraData1.ModificationTime;
                else if (UnixExtraData2 != null)
                    LastModificationTime = UnixExtraData2.LastModificationTime;
                else if (WindowsExtraData != null)
                    LastModificationTime = WindowsExtraData.LastModificationTime;
                else
                    LastModificationTime = zipEntry.GetLastModificationTime();

                if (!string.IsNullOrEmpty(zipEntry.Name))
                {
                    var unicodeEntryNameExtraData = extraData.GetData<UnicodePathExtraField>();
                    if (unicodeEntryNameExtraData != null)
                    {
                        // 本来は zipEntry.Name の元になったバイト列の CRC を計算しなければならない。
                        var crc = Encoding.Default.GetBytes(zipEntry.Name).CalculateCRC32();
                        if (crc == unicodeEntryNameExtraData.CRC32)
                            FullName = unicodeEntryNameExtraData.FullName;
                        else
                        {
                            // NOP
                        }
                    }
                }
                if (!string.IsNullOrEmpty(zipEntry.Comment))
                {
                    var unicodeCommentExtraData = extraData.GetData<UnicodeCommentExtraField>();
                    if (unicodeCommentExtraData != null)
                    {
                        // 本来は zipEntry.Comment の元になったバイト列の CRC を計算しなければならない。
                        var crc = Encoding.Default.GetBytes(zipEntry.Comment).CalculateCRC32();
                        if (crc == unicodeCommentExtraData.CRC32)
                            Comment = unicodeCommentExtraData.Comment;
                        else
                        {
                            // NOP
                        }
                    }
                }
            }
        }
#else
        public CabinetEntryProperty(string entryTechnicalInformationText)
        {
            var dic =
                entryTechnicalInformationText.Split('\n')
                .Select(line => new { text = line, match = _keyValuePattern.Match(line) })
                .Select(item =>
                {
                    if (!item.match.Success)
                        throw new Exception(string.Format("書庫のエントリ情報が解析できません。: text=\"{0}\"", item.text));
                    return new { key = item.match.Groups["key"].Value.Trim(), value = item.match.Groups["value"].Value.Trim() };
                })
                .ToDictionary(item => item.key, item => item.value);
            FullName = dic["Path"];
            IsDirectory = dic["Folder"] == "+";
            EntryNameEncoding =
                dic["Characteristics"].Split(':')
                .Select(column => column.Trim())
                .Any(value => string.Equals(value, "UTF8", StringComparison.InvariantCultureIgnoreCase))
                ? CabinetEntryNameEncoding.UTF8
                : CabinetEntryNameEncoding.Local;
            Offset = long.Parse(dic["Offset"], NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat);
            Modified = DateTime.Parse(dic["Modified"], CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeLocal);
            CRC = string.IsNullOrEmpty(dic["CRC"]) ? 0 : uint.Parse(dic["CRC"], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture.NumberFormat);
    }
#endif
        public bool IsDirectory { get; }
        public string FullName { get; }
        public string Comment { get; }
        public long Offset { get; }
        public DateTime LastModificationTime { get; }
        public long CRC { get; }
        public long Size { get; }
        public long PackedSize { get; }
        public int HostSystem { get; }
        public int ExternalFileAttributes { get; }
        public ZipArchiveEntryTextEncoding EntryTextEncoding { get; }
        public byte[] ExtraData { get; }
        public NTTaggedData WindowsExtraData { get; }
        public ExtendedUnixData UnixExtraData1 { get; }
        public UnixExtraField UnixExtraData2 { get; }
        public UnicodePathExtraField UnicodeEntryNameExtraData { get; }
        public UnicodeCommentExtraField UnicodeCommentExtraData { get; }
    }
}
