using ICSharpCode.SharpZipLib.Zip;
using System;

namespace ZipArchiveNormalizer
{
    static class ZipEntryExtensions
    {
        public static DateTime GetLastModificationTime(this ZipEntry entry)
        {
            // entry.DateTime の Kind は Unspecified だが、 Local とみなして取得する
            return new DateTime(
                entry.DateTime.Year,
                entry.DateTime.Month,
                entry.DateTime.Day,
                entry.DateTime.Hour,
                entry.DateTime.Minute,
                entry.DateTime.Second,
                DateTimeKind.Local);
        }

        public static void SetLastModificationTime(this ZipEntry entry, DateTime datetime)
        {
#if DEBUG
            if (datetime.ToLocalTime() != datetime.ToLocalTime().ToLocalTime())
                throw new Exception();
#endif
            // entry.DateTime 内では Kind は参照されずに DosTime の計算に使用されているので、代入前にあらかじめ Local Time に変更しておく。
            entry.DateTime = datetime.ToLocalTime();
        }
    }
}
