using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ZipArchiveNormalizer
{
    class ZipArchive
    {
        private class ZipArchiveNodeWorkingItem
        {
            public ZipArchiveNodeWorkingItem(ZipArchiveEntry entry, IEnumerable<string> names)
            {
                Entry = entry;
                FirstName = names.First();
                OtherNames = names.Skip(1).ToList();
            }

            public ZipArchiveEntry Entry { get; }
            public string FirstName { get; }
            public IEnumerable<string> OtherNames { get; }
            public bool IsFile => !OtherNames.Any();
        }

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

#if true
#else
        private const string _archiverCommandName = @"7z.exe";
        private static Regex _listCommandResultPattern;
#endif

        static ZipArchive()
        {
#if true
#else
            _listCommandResultPattern = new Regex(@"----------\s*(?<info>.*)$", RegexOptions.Compiled | RegexOptions.Singleline);
#endif
        }

        public ZipArchive(string cabinetPath)
        {
            FullName = cabinetPath;
        }

        public string FullName { get; }

        public IEnumerable<ZipArchiveEntry> GetEntries()
        {
#if true
            RaiseProgressChanged(string.Format("\"{0}\": アーカイブの一覧の取得を開始しました。", FullName));
            return new ZipFileEntryEnumerable(FullName);
#else
            var result = ExecuteCommand(FullName, "l", new[] { "-sns-", "-slt" }).Replace("\r\n", "\n");
            RaiseProgressChanged(string.Format("\"{0}\": アーカイブの一覧を取得しました。", FullName));
            var infoMatch = _listCommandResultPattern.Match(result);
            if (!infoMatch.Success)
                throw new Exception(string.Format("コマンドの実行結果が解析できません。:\n{0}", result));
            return
                infoMatch.Groups["info"].Value.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => new CabinetEntryProperty(item))
                .OrderBy(item => item.Offset)
                .ToList();
#endif
        }

        public ZipArchiveEntryTreeRootDirectory GetEntryTree()
        {
            var entries = GetEntries();
            var fileEntries = entries.Where(item => item.IsDirectory == false);
            var workList =
                fileEntries.
                Select(item => new ZipArchiveNodeWorkingItem(item, item.FullName.Split(new[] { '/', '\\' })));
            return ConstructRootDirectoryTree(entries, workList);
        }

        public bool Test()
        {
#if true
            RaiseProgressChanged(string.Format("\"{0}\": アーカイブのテストをします。", FullName));
            try
            {
                using (var zipFile = new ZipFile(FullName))
                {
                    return zipFile.TestArchive(true);
                }
            }
            catch (Exception)
            {
                return false;
            }
#else
            try
            {
                ExecuteCommand(FullName, "t", new[] { "*", "-sns-", "-r" });
                RaiseProgressChanged(string.Format("\"{0}\": アーカイブのテストをしました。", FullName));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
#endif
        }

        public void RaiseProgressChanged(string message)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, new ProgressChangedEventArgs(message));
        }

        private ZipArchiveEntryTreeRootDirectory ConstructRootDirectoryTree(IEnumerable<ZipArchiveEntry> entries, IEnumerable<ZipArchiveNodeWorkingItem> children)
        {
            var directory = new ZipArchiveEntryTreeRootDirectory(this, entries);
            BuildDirectoryTree(directory, children);
            return directory;
        }

        private static void BuildDirectoryTree(ZipArchiveEntryTreeDirectory directory, IEnumerable<ZipArchiveNodeWorkingItem> children)
        {
            var elements =
                children
                .GroupBy(item => item.FirstName)
                .Select(g =>
                {
                    var childName = g.Key;
                    if (g.Any(item => item.IsFile))
                    {
                        var file = g.Single();
                        return new ZipArchiveEntryTreeFile(file.FirstName, file.Entry) as ZipArchiveEntryTreeNode;
                    }
                    else
                    {
                        if (g.Any(item => item.IsFile))
                            throw new Exception("ファイル名が重複しています。");
                        var childDirectory = new ZipArchiveEntryTreeDirectory(childName);
                        BuildDirectoryTree(childDirectory, g.Select(item => new ZipArchiveNodeWorkingItem(item.Entry, item.OtherNames)));
                        return childDirectory as ZipArchiveEntryTreeNode;
                    }
                });
            foreach (var element in elements)
                directory.AddChild(element);
        }

#if true
#else
        public void ExtractEntry(string baseDirectoryPath)
        {
            ExecuteCommand(FullName, baseDirectoryPath, "x", new[] { "-sns-", "-mtc=on", "-mcu=on" });
            RaiseProgressChanged(string.Format("\"{0}\": アーカイブのテストからファイルを取得しました。", FullName));
        }

        public void AddEntry(string baseDirectoryPath, string localFilePath)
        {
            ExecuteCommand(FullName, baseDirectoryPath, "a", new[] { string.Format("\"{0}\"", localFilePath), "-mcu=on", "-mtc=on", "-sns-", "-stl", "-tzip" });
            RaiseProgressChanged(string.Format("\"{0}\": アーカイブにファイルを追加しました。: \"{1}\"", FullName, localFilePath));
        }

        private static string ExecuteCommand(string cabinetFilePath, string command, string[] options)
        {
            return ExecuteCommand(cabinetFilePath, Environment.CurrentDirectory, command, options);
        }

        private static string ExecuteCommand(string cabinetFilePath, string workingDirectoryPath, string command, string[] options)
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo(_archiverCommandName, string.Join(" ", new[] { command, string.Format("\"{0}\"", cabinetFilePath) }.Concat(options)))
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.GetEncoding("shift_jis"),
                UseShellExecute = false,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                WorkingDirectory = workingDirectoryPath,
            };
            var process = System.Diagnostics.Process.Start(startInfo);
            var result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new Exception(string.Format("コマンドが異常終了しました。: exit-code={0}, command-line=\"{1} {2}\"", process.ExitCode, _archiverCommandName, startInfo.Arguments));
            return result;
        }
#endif
    }
}
