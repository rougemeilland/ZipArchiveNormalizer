using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ZipArchiveNormalizer
{
    class Program
    {
        private static IComparer<string> _entryPathComparer;
        private static IEqualityComparer<string> _zipArchiveFilePathEqualityComparer;
        private static int _state = 0;

        static Program()
        {
            _entryPathComparer = new ZipArchiveEntryFilePathNameComparer(ZipArchiveEntryFilePathNameComparerrOption.None);
            _zipArchiveFilePathEqualityComparer = new ZipArchiveFilePathEqualityComparer();
        }

        static void Main(string[] args)
        {
#if false
#else
            var badArchiveFiles = new List<string>();

            // 正規化の実行
            Walk(args, zipArchiveFilePath =>
            {
                try
                {
                    var zipArchive = new ZipArchive(zipArchiveFilePath);
                    zipArchive.ProgressChanged += ZipArchive_ProgressChanged;
                    try
                    {
                        if (zipArchive.Test())
                            NormalizeZipArchive(zipArchive);
                        else
                            badArchiveFiles.Add(zipArchiveFilePath);
                    }
                    finally
                    {
                        zipArchive.ProgressChanged -= ZipArchive_ProgressChanged;
                    }
                }
                catch (IOException)
                {
                }
            });

            DeleteSimilarFiles(args, badArchiveFiles);
            NormalizeZipArchiveFileNames(args, badArchiveFiles);
#endif
            Console.WriteLine("終了しました。Enterを押してください。");
            Console.ReadLine();
        }

        private static void NormalizeZipArchive(ZipArchive zipArchive)
        {
            if (!zipArchive.Test())
            {
                Console.WriteLine(string.Format("\"{0}\": 書庫が壊れているので無視します。", zipArchive.FullName));
                return;
            }
            var rootTree = zipArchive.GetEntryTree();
            rootTree.Reported += (s, e) =>
            {
                Console.WriteLine(string.Format("\"{0}\": {1}", zipArchive.FullName, e.Message));
            };
            bool needToUpdate = rootTree.Normalize(new Regex(@"^(.*\.(exe|scr|com|vbs|folder|jar|db|vix|sue|ion|ini|url|jbf|lst|pe4|dvi|htm|html|mht|mhtl|rtf)|\.DS_Store)$"));
            if (rootTree.IsEmpty)
            {
                File.Delete(zipArchive.FullName);
                Console.WriteLine(string.Format("\"{0}\": 書庫が空なので書庫ファイルを削除します。", zipArchive.FullName));
            }
            else if (needToUpdate)
            {
                var newZipArchiveFilePath = Path.Combine(Path.GetDirectoryName(zipArchive.FullName), "." + Path.GetFileName(zipArchive.FullName) + ".temp.zip");
                try
                {
                    File.Delete(newZipArchiveFilePath);
                    var newZipArchivet = new ZipArchive(newZipArchiveFilePath);
                    newZipArchivet.ProgressChanged += ZipArchive_ProgressChanged;
                    try
                    {
                        rootTree.SaveTo(newZipArchivet);
                        FileSystem.DeleteFile(zipArchive.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        File.Move(newZipArchiveFilePath, zipArchive.FullName);
                    }
                    finally
                    {
                        newZipArchivet.ProgressChanged -= ZipArchive_ProgressChanged;
                    }
                }
                catch (Exception)
                {
                    File.Delete(newZipArchiveFilePath);
                    throw;
                }
            }
            else
            {
                // NOP
            }
        }

        private static void DeleteSimilarFiles(string[] args, IEnumerable<string> badArchiveFiles)
        {
            var zipArchiveFiles = new List<FileInfo>();
            Walk(args, zipArchiveFilePath =>
            {
                ZipArchive_ProgressChanged(null, new ProgressChangedEventArgs(string.Format("\"{0}\": アーカイブの情報を集めています。", zipArchiveFilePath)));
                if (!badArchiveFiles.Contains(zipArchiveFilePath, _zipArchiveFilePathEqualityComparer))
                    zipArchiveFiles.Add(new FileInfo(zipArchiveFilePath));
            });

            var groupedZipArchiveFiles =
                zipArchiveFiles
                .GroupBy(file => file.Length)
                .Select(g => new
                {
                    length = g.Key,
                    zipArchibeFiles = g.ToList(),
                })
                .ToList();
            foreach (var groupedZipArchiveFile in groupedZipArchiveFiles)
            {
                var zipArchiveFileInfos =
                    groupedZipArchiveFile.zipArchibeFiles
                    .Select(file =>
                    {
                        try
                        {
                            var zipArchive = new ZipArchive(file.FullName);
                            zipArchive.ProgressChanged += ZipArchive_ProgressChanged;
                            try
                            {
                                return new { zipArchiveFilePath = file.FullName, entries = zipArchive.GetEntries().ToList() };
                            }
                            finally
                            {
                                zipArchive.ProgressChanged -= ZipArchive_ProgressChanged;
                            }
                        }
                        catch (Exception)
                        {
                            return null;
                        }
                    })
                    .ToList();
                while (zipArchiveFileInfos.Count >= 2)
                {
                    var zipArchiveFileInfo1 = zipArchiveFileInfos.First();
                    string removedZipArchiveFilePath = null;
                    string otherZipArchiveFilePath = null;
                    foreach (var zipArchiveFileInfo2 in zipArchiveFileInfos.Skip(1))
                    {
                        ZipArchive_ProgressChanged(null, new ProgressChangedEventArgs(string.Format("\"{0}\": 内容が同じアーカイブがないか調べています。", zipArchiveFileInfo1.zipArchiveFilePath)));
                        if (zipArchiveFileInfo1.entries.Count == zipArchiveFileInfo2.entries.Count)
                        {
                            var entryPairs = zipArchiveFileInfo1.entries.Zip(zipArchiveFileInfo2.entries, (entry1, entry2) => new { entry1, entry2 });
                            if (entryPairs
                                .All(item =>
                                    _entryPathComparer.Compare(item.entry1.FullName, item.entry2.FullName) == 0 &&
                                    item.entry1.Offset == item.entry2.Offset &&
                                    item.entry1.Size == item.entry2.Size &&
                                    item.entry1.CRC == item.entry2.CRC &&
                                    item.entry1.ExtraData.EqualsByteArray(item.entry2.ExtraData)))
                            {
                                // エントリのパス、サイズ、オフセット、CRCがすべて一致している場合

                                var zipArchiveFile1TimeStamp = zipArchiveFileInfo1.entries.Max(entry => entry.LastModificationTime.ToUniversalTime().Ticks);
                                var zipArchiveFile2TimeStamp = zipArchiveFileInfo2.entries.Max(entry => entry.LastModificationTime.ToUniversalTime().Ticks);
                                if (zipArchiveFile1TimeStamp > zipArchiveFile2TimeStamp)
                                {
                                    removedZipArchiveFilePath = zipArchiveFileInfo1.zipArchiveFilePath;
                                    otherZipArchiveFilePath = zipArchiveFileInfo2.zipArchiveFilePath;
                                }
                                else if (zipArchiveFile1TimeStamp < zipArchiveFile2TimeStamp)
                                {
                                    removedZipArchiveFilePath = zipArchiveFileInfo2.zipArchiveFilePath;
                                    otherZipArchiveFilePath = zipArchiveFileInfo1.zipArchiveFilePath;
                                }
                                else if (zipArchiveFileInfo1.zipArchiveFilePath.Length >= zipArchiveFileInfo2.zipArchiveFilePath.Length)
                                {
                                    removedZipArchiveFilePath = zipArchiveFileInfo1.zipArchiveFilePath;
                                    otherZipArchiveFilePath = zipArchiveFileInfo2.zipArchiveFilePath;
                                }
                                else
                                {
                                    removedZipArchiveFilePath = zipArchiveFileInfo2.zipArchiveFilePath;
                                    otherZipArchiveFilePath = zipArchiveFileInfo1.zipArchiveFilePath;
                                }
                                break;
                            }
                        }
                    }
                    if (removedZipArchiveFilePath != null)
                    {
                        FileSystem.DeleteFile(removedZipArchiveFilePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        Console.WriteLine(string.Format("\"{0}\": エントリのタイムスタンプだけが異なる別のアーカイブファイルが存在するので、アーカイブファイルを削除しました。: \"{1}\"", removedZipArchiveFilePath, otherZipArchiveFilePath));
                        zipArchiveFileInfos =
                        zipArchiveFileInfos
                        .Where(item => !_zipArchiveFilePathEqualityComparer.Equals(item.zipArchiveFilePath, removedZipArchiveFilePath))
                        .ToList();
                    }
                    else
                        zipArchiveFileInfos = zipArchiveFileInfos.Skip(1).ToList();
                }
            }
        }

        private static void NormalizeZipArchiveFileNames(string[] args, IEnumerable<string> badArchiveFiles)
        {
            var pathPattern = new Regex(@"^(?<path>.*?)(\s*\([0-9]+\))+$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            Walk(args, zipArchiveFilePath =>
            {
                ZipArchive_ProgressChanged(null, new ProgressChangedEventArgs(string.Format("\"{0}\": アーカイブのファイル名が短縮できないか調べています。", zipArchiveFilePath)));
                if (!badArchiveFiles.Contains(zipArchiveFilePath, _zipArchiveFilePathEqualityComparer))
                {
                    var zipArchiveFileName = Path.GetFileNameWithoutExtension(Path.GetFileName(zipArchiveFilePath)).Replace("　", " ").Replace("（", "(").Replace("）", ")").Trim();
                    if (!zipArchiveFileName.Contains("？"))
                        zipArchiveFileName = zipArchiveFileName.Replace("！", "!");
                    var zipArchiveFileDirectory = Path.GetDirectoryName(zipArchiveFilePath);
                    var fileNameMatch = pathPattern.Match(zipArchiveFileName);
                    if (fileNameMatch.Success)
                        zipArchiveFileName = fileNameMatch.Groups["path"].Value;
                    var retryCount = 1;
                    while (true)
                    {
                        var newFilePath =
                            Path.Combine(
                            zipArchiveFileDirectory,
                            zipArchiveFileName + (retryCount <= 1 ? "" : string.Format(" ({0})", retryCount)) + ".zip");
                        if (string.Equals(newFilePath, zipArchiveFilePath, StringComparison.InvariantCultureIgnoreCase))
                            break;
                        else if (!File.Exists(newFilePath))
                        {
                            File.Move(zipArchiveFilePath, newFilePath);
                            Console.WriteLine(string.Format("\"{0}\": アーカイブのファイル名を短縮しました。: \"{1}\"", zipArchiveFilePath, Path.GetFileName(newFilePath)));
                            break;
                        }
                        else
                            ++retryCount;
                    }
                }
            });
        }

        private static void Walk(string[] args, Action<string> action)
        {
            foreach (var path in args)
            {
                FileInfo file = null;
                DirectoryInfo directory = null;
                try
                {
                    file = new FileInfo(path);
                    if (!file.Exists || !string.Equals(file.Extension, ".zip", StringComparison.InvariantCultureIgnoreCase))
                        file = null;
                }
                catch (IOException)
                {
                    file = null;
                }
                try
                {
                    directory = new DirectoryInfo(path);
                    if (!directory.Exists)
                        directory = null;
                }
                catch (IOException)
                {
                    directory = null;
                }
                if (file != null)
                    action(file.FullName);
                else if (directory != null)
                {
                    foreach (var childFile in directory.EnumerateFiles("*.zip", System.IO.SearchOption.AllDirectories))
                        action(childFile.FullName);
                }
                else
                {
                    // NOP
                }
            }
        }

        private static void ZipArchive_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            switch (_state)
            {
                case 0:
                    Console.Write("─\r");
                    _state = 1;
                    break;
                case 1:
                    Console.Write("＼\r");
                    _state = 2;
                    break;
                case 2:
                    Console.Write("｜\r");
                    _state = 3;
                    break;
                default:
                    Console.Write("／\r");
                    _state = 0;
                    break;
            }
        }
    }
}