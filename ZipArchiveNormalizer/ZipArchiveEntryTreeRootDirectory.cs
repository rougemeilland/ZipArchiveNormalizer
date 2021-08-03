using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ZipArchiveNormalizer
{
    class ZipArchiveEntryTreeRootDirectory
        : ZipArchiveEntryTreeDirectory
    {
        public event EventHandler<ReportedEventArgs> Reported;

        private static IComparer<string> _entryPathComparer;
        private ZipArchive _zipArchive;
        private IEnumerable<ZipArchiveEntry> _entries;

        static ZipArchiveEntryTreeRootDirectory()
        {
            _entryPathComparer = new ZipArchiveEntryFilePathNameComparer(ZipArchiveEntryFilePathNameComparerrOption.None);
        }

        public ZipArchiveEntryTreeRootDirectory(ZipArchive zipArchive, IEnumerable<ZipArchiveEntry> entries)
            : base(null)
        {
            _zipArchive = zipArchive;
            _entries = entries;
        }

        public bool Normalize(Regex excludedFilePattern)
        {
            var needToUpdate = false;
            if (!needToUpdate)
            {
                var directoryPathList =
                    _entries
                    .Where(entry => entry.IsDirectory == true)
                    .Select(entry =>
                    {
                        var path = entry.FullName.Replace('\\', '/');
                        return path.EndsWith("/") ? path : path + "/";
                    } )
                    .ToList();
                var filePathList =
                    _entries
                    .Where(entry => entry.IsDirectory == false)
                    .Select(entry => entry.FullName.Replace('\\', '/'))
                    .ToList();
                var emptyDirectories =
                    directoryPathList
                    .Where(directoryPath => !filePathList.Any(filePath => filePath.StartsWith(directoryPath, StringComparison.InvariantCultureIgnoreCase)));
                if (emptyDirectories.Any())
                {
                    if (!needToUpdate)
                        RaiseReportedEvent(string.Format("空のフォルダエントリが含まれているので削除します。: \"{0}\", ...", emptyDirectories.First()));
                    needToUpdate = true;
                }
            }
            if (RemoveUselessFileEntry(excludedFilePattern, null, m => RaiseReportedEvent(m)))
                needToUpdate = true;
            if (RemoveUselessDirectoryEntry(null, m => RaiseReportedEvent(m)))
                needToUpdate = true;
            if (NormalizePageNumber(null, m => RaiseReportedEvent(m)))
                needToUpdate = true;
            SortEntries();
            if (!needToUpdate)
            {
                var fileEntryCount = 0;
                Walk((entry, newPath) =>
                {
                    ++fileEntryCount;
                }, null);

                if (fileEntryCount != _entries.Where(entry => entry.IsDirectory == false).Count())
                {
                    if (!needToUpdate)
                        RaiseReportedEvent(string.Format("削除されたエントリがあるのでアーカイブを変更します。"));
                    needToUpdate = true;
                }
            }
            if (!needToUpdate)
            {
                Walk((entry, newPath) =>
                {
                    if (_entryPathComparer.Compare(entry.FullName, newPath) != 0)
                    {
                        if (!needToUpdate)
                            RaiseReportedEvent(string.Format("エントリのパス名または順序が変更されているのでアーカイブを変更します。"));
                        needToUpdate = true;
                    }
                }, null);
            }
            if (!needToUpdate)
            {
                var entryList = new List<ZipArchiveEntry>();
                Walk((entry, newPath) => entryList.Add(entry), null);
                entryList
                    .Aggregate(
                        -1L,
                        (previoudOffset, entry) =>
                        {
                            if (previoudOffset > entry.Offset)
                            {
                                if (!needToUpdate)
                                    RaiseReportedEvent(string.Format("エントリの順番を最適化するためにアーカイブを変更します。"));
                                needToUpdate = true;
                            }
                            return entry.Offset;
                        });
            }
            if (!needToUpdate)
            {
                string found = null;
                Walk((entry, newPath) =>
                {
                    if (found == null)
                    {
                        if (entry.EntryTextEncoding == ZipArchiveEntryTextEncoding.Local)
                        {
                            var bytes1 = Encoding.UTF8.GetBytes(entry.FullName);
                            var bytes2 = Encoding.ASCII.GetBytes(entry.FullName);
                            if (bytes1.Length != bytes2.Length)
                                found = newPath;
                            else if (bytes1.Zip(bytes2, (byte1, byte2) => new { byte1, byte2 }).Any(item => item.byte1 != item.byte2))
                                found = newPath;
                            else
                            {
                                // NOP
                            }
                        }
                        else
                        {
                            // NOP
                        }
                    }
                }, null);
                if (found != null)
                {
                    RaiseReportedEvent(string.Format("UTF8エンコーディングではないエントリ名が含まれているのでUTF8エンコーディングに変更します。: \"{0}\", ...", found));
                    needToUpdate = true;
                }
            }
            return needToUpdate;
        }

        public void SaveTo(ZipArchive destnationZipArchive)
        {
#if true
            _zipArchive.RaiseProgressChanged(string.Format("\"{0}\": アーカイブの正規化を開始します。", _zipArchive.FullName));

            var fileTimeStamp = (DateTime?)null;
            using (var sourceZipFile = new ZipFile(_zipArchive.FullName))
            using (var newZipArchiveFileStrem = new FileStream(destnationZipArchive.FullName, FileMode.Create))
            using (var newZipArchiveOutputStream = new ZipOutputStream(newZipArchiveFileStrem))
            {
                // zip ファイルコメントの設定
                newZipArchiveOutputStream.SetComment(sourceZipFile.ZipFileComment);

                // ファイルのコピー
                Walk((entry, newEntryPath) =>
                {
                    _zipArchive.RaiseProgressChanged(string.Format("\"{0}\": アーカイブのエントリのコピーを開始します。: \"{1}\"", _zipArchive.FullName, newEntryPath));

                    var newEntry = new ZipEntry(newEntryPath);

                    // 基本属性の設定 (文字コード系は強制的に UTF-8 に変更する)
                    newEntry.IsUnicodeText = true;
                    newEntry.Comment = entry.Comment;
                    newEntry.HostSystem = entry.HostSystem;
                    newEntry.ExternalFileAttributes = entry.ExternalFileAttributes;
                    newEntry.Size = entry.Size;

                    // 更新日付の設定
                    newEntry.SetLastModificationTime(entry.LastModificationTime);

                    // アーカイブファイルの日付の更新 (全エントリ内で最も新しい更新日付を見つける)
                    if (fileTimeStamp == null || fileTimeStamp.Value < entry.LastModificationTime)
                        fileTimeStamp = entry.LastModificationTime;

                    // extra data の設定
                    using (var newExtraData = new ZipExtraData(newEntry.ExtraData))
                    {
                        if (entry.WindowsExtraData != null)
                            newExtraData.AddEntry(entry.WindowsExtraData);
                        if (entry.UnixExtraData1 != null)
                            newExtraData.AddEntry(entry.UnixExtraData1);
                        if (entry.UnixExtraData2 != null)
                            newExtraData.AddEntry(entry.UnixExtraData2);
                        newEntry.ExtraData = newExtraData.GetEntryData();
                    }

                    // アーカイブファイルへの書き込み
                    newZipArchiveOutputStream.PutNextEntry(newEntry);
                    var currentEntry = new ZipEntry(entry.FullName);
                    using (var sourceZipArchiveInputStrem = sourceZipFile.GetInputStream(currentEntry))
                    {
                        var buffer = new byte[64 * 1024];
                        while (true)
                        {
                            var length = sourceZipArchiveInputStrem.Read(buffer, 0, buffer.Length);
                            if (length <= 0)
                                break;
                            newZipArchiveOutputStream.Write(buffer, 0, length);
                        }
                    }
                }, null);
            }

            // ターゲット zip ファイルの更新日付を変更する
            if (fileTimeStamp.HasValue)
                File.SetLastWriteTimeUtc(destnationZipArchive.FullName, fileTimeStamp.Value.ToUniversalTime());
#else
            using (var tempDirectory = TemporaryDirectory.Create())
            {
                var sourceDirectory = Path.Combine(tempDirectory.FullName, "s");
                Directory.CreateDirectory(sourceDirectory);
                var destinationDirectory = Path.Combine(tempDirectory.FullName, "d");
                _cabinet.ExtractEntry(sourceDirectory);
                Walk((entry, newEntryPath) =>
                {
                    var sourceFilePath = Path.Combine(sourceDirectory, entry.FullName);
                    var destinationFilePath = Path.Combine(destinationDirectory, newEntryPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath));
                    File.Move(sourceFilePath, destinationFilePath);
                    destnationCabinet.AddEntry(destinationDirectory, newEntryPath);
                }, null);
            }
#endif
        }

        public bool IsEmpty => !Children.Any();

        private void RaiseReportedEvent(string message)
        {
            if (Reported != null)
                Reported(this, new ReportedEventArgs(message));
        }
    }
}
