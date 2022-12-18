using System;
using System.Collections.Generic;
using System.IO;

namespace ZipArchiveNormalizer
{
    /// <summary>
    /// 内容が同じ二つのファイルのうちどちらの重要度が高いかを比較する <see cref="IComparable{FileInfo}">IComparable&lt;<see cref="FileInfo"/>&gt;</see> の実装
    /// </summary>
    class FileImportanceComparer
        : IComparer<FileInfo>
    {
        public Int32 Compare(FileInfo? x, FileInfo? y)
        {
            if (x is null)
                return y is null ? 0 : -1;
            else if (y is null)
                return 1;
            else
                return CompareFineInfo(x, y);
        }

        private static Int32 CompareFineInfo(FileInfo x, FileInfo y)
        {
            Int32 c;
            // アーカイブファイルの更新日付が古い方が重要
            if ((c = x.LastWriteTimeUtc.CompareTo(y.LastWriteTimeUtc)) != 0)
                return -c;

            // アーカイブファイルの作成日付が古い方が重要
            if ((c = x.CreationTimeUtc.CompareTo(y.CreationTimeUtc)) != 0)
                return -c;

            // アーカイブファイルのファイル名が長い方が重要
            if ((c = x.Name.Length.CompareTo(y.Name.Length)) != 0)
                return c;

            // アーカイブファイルのフルパス名が短い方が重要
            if ((c = x.FullName.Length.CompareTo(y.FullName.Length)) != 0)
                return -c;

            // ほかに比較すべき条件はないが、比較結果を確定させるためにファイルパス名自体の比較結果を返す。
            return x.FullName.CompareTo(y.FullName);
        }
    }
}
