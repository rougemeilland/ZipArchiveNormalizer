﻿using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utility;
using Utility.FileWorker;
using ZipUtility.ZipExtraField;

namespace Support.CharacteristicZipFilesCollecter
{
    class Program
    {
        private class Walker
            : FileWorkerFromMainArgument
        {
            private bool _ok_hugeFile;
            private bool _ok_dataDescriptor;
            private bool _ok_zip64;
            private bool _ok_UT;
            private bool _ok_SD;
            private bool _ok_up;
            private bool _ok_NTFS;

            public Walker(IWorkerCancellable canceller)
                : base(canceller, FileWorkerConcurrencyMode.ParallelProcessingForEachFile)
            {
                _ok_hugeFile = false;
                _ok_dataDescriptor = false;
                _ok_zip64 = false;
                _ok_UT = false;
                _ok_SD = false;
                _ok_up = false;
                _ok_NTFS = false;
            }

            public override string Description => "テストに役立ちそうなZIPファイルを収集します。";

            protected override IFileWorkerActionFileParameter? IsMatchFile(FileInfo sourceFile)
            {
                return
                    sourceFile.Extension.IsAnyOf(".zip", ".epub", StringComparison.OrdinalIgnoreCase)
                        ? base.IsMatchFile(sourceFile)
                        : null;
            }

            protected override void ActionForFile(FileInfo sourceFile, IFileWorkerActionParameter parameter)
            {
                try
                {
                    if (sourceFile.Length > UInt32.MaxValue)
                    {
                        lock (this)
                        {
                            var targetFilePath = Path.Combine(@"D:\テストデータ", "大きなZIPファイル.zip");
                            if (!File.Exists(targetFilePath))
                            {
                                File.Delete(targetFilePath);
                                sourceFile.CopyTo(targetFilePath);
                                File.SetAttributes(targetFilePath, FileAttributes.ReadOnly);
                                RaiseInformationReportedEvent(
                                    sourceFile,
                                    string.Format("ファイルを見つけました。: {0}",
                                        Path.GetFileNameWithoutExtension(targetFilePath)));
                            }
                            _ok_hugeFile = true;
                        }
                    }
                    using var zipFile = new ZipFile(sourceFile.FullName);
                    foreach (var entry in zipFile.Cast<ZipEntry>())
                    {
                        if (((GeneralBitFlags)entry.Flags).HasFlag(GeneralBitFlags.Descriptor))
                        {
                            var targetFilePath = Path.Combine(@"D:\テストデータ", "データディスクリプタがあるZIPファイル.zip");
                            lock (this)
                            {
                                if (!File.Exists(targetFilePath))
                                {
                                    File.Delete(targetFilePath);
                                    sourceFile.CopyTo(targetFilePath);
                                    File.SetAttributes(targetFilePath, FileAttributes.ReadOnly);
                                    RaiseInformationReportedEvent(
                                        sourceFile,
                                        string.Format("ファイルを見つけました。: {0}",
                                            Path.GetFileNameWithoutExtension(targetFilePath)));
                                }
                                _ok_dataDescriptor = true;
                            }
                        }
                        using (var extraData = new ZipExtraData(entry.ExtraData))
                        {
                            if (extraData.Find(1) || entry.Size > UInt32.MaxValue)
                            {
                                lock (this)
                                {
                                    var targetFilePath = Path.Combine(@"D:\テストデータ", "Z64形式のZIPファイル.zip");
                                    if (!File.Exists(targetFilePath))
                                    {
                                        File.Delete(targetFilePath);
                                        sourceFile.CopyTo(targetFilePath);
                                        File.SetAttributes(targetFilePath, FileAttributes.ReadOnly);
                                        RaiseInformationReportedEvent(
                                            sourceFile,
                                            string.Format("ファイルを見つけました。: {0}",
                                                Path.GetFileNameWithoutExtension(targetFilePath)));
                                    }
                                    _ok_zip64 = true;
                                }
                            }
                            if (extraData.Find(0x5455) && !extraData.Find(10))
                            {
                                var unix = extraData.GetData<ExtendedUnixData>();
                                if (unix.Include == (ExtendedUnixData.Flags.AccessTime | ExtendedUnixData.Flags.CreateTime | ExtendedUnixData.Flags.ModificationTime))
                                {
                                    lock (this)
                                    {
                                        var targetFilePath = Path.Combine(@"D:\テストデータ", "UNIXタイムスタンプがあるZIPファイル.zip");
                                        if (!File.Exists(targetFilePath))
                                        {
                                            File.Delete(targetFilePath);
                                            sourceFile.CopyTo(targetFilePath);
                                            File.SetAttributes(targetFilePath, FileAttributes.ReadOnly);
                                            RaiseInformationReportedEvent(
                                                sourceFile,
                                                string.Format("ファイルを見つけました。: {0}",
                                                    Path.GetFileNameWithoutExtension(targetFilePath)));
                                        }
                                        _ok_UT = true;
                                    }
                                }
                            }
                            if (extraData.Find(10) && !extraData.Find(0x5455))
                            {
                                lock (this)
                                {
                                    var targetFilePath = Path.Combine(@"D:\テストデータ", "NTFSタイムスタンプがあるZIPファイル.zip");
                                    if (!File.Exists(targetFilePath))
                                    {
                                        File.Delete(targetFilePath);
                                        sourceFile.CopyTo(targetFilePath);
                                        File.SetAttributes(targetFilePath, FileAttributes.ReadOnly);
                                        RaiseInformationReportedEvent(
                                            sourceFile,
                                            string.Format("ファイルを見つけました。: {0}",
                                                Path.GetFileNameWithoutExtension(targetFilePath)));
                                    }
                                    _ok_NTFS = true;
                                }
                            }
                            if (extraData.Find(0x4453))
                            {
                                lock (this)
                                {
                                    var targetFilePath = Path.Combine(@"D:\テストデータ", "WindowsセキュリティディスクリプタがあるZIPファイル.zip");
                                    if (!File.Exists(targetFilePath))
                                    {
                                        File.Delete(targetFilePath);
                                        sourceFile.CopyTo(targetFilePath);
                                        File.SetAttributes(targetFilePath, FileAttributes.ReadOnly);
                                        RaiseInformationReportedEvent(
                                            sourceFile,
                                            string.Format("ファイルを見つけました。: {0}",
                                                Path.GetFileNameWithoutExtension(targetFilePath)));
                                    }
                                    _ok_SD = true;
                                }
                            }
                            if (extraData.Find(0x7075))
                            {
                                var extraField = extraData.GetData<UnicodePathExtraField>();
                                if (extraField is not null && !string.Equals(entry.Name, extraField.FullName, StringComparison.OrdinalIgnoreCase))
                                {
                                    lock (this)
                                    {
                                        var targetFilePath = Path.Combine(@"D:\テストデータ", "UNICODEパス拡張があるZIPファイル.zip");
                                        if (!File.Exists(targetFilePath))
                                        {
                                            File.Delete(targetFilePath);
                                            sourceFile.CopyTo(targetFilePath);
                                            File.SetAttributes(targetFilePath, FileAttributes.ReadOnly);
                                            RaiseInformationReportedEvent(
                                                sourceFile,
                                                string.Format("ファイルを見つけました。: {0}",
                                                    Path.GetFileNameWithoutExtension(targetFilePath)));
                                        }
                                        _ok_up = true;
                                    }
                                }
                            }
                        }
                        UpdateProgress();
                        if (_ok_hugeFile &&
                            _ok_dataDescriptor &&
                            _ok_zip64 &&
                            _ok_UT &&
                            _ok_SD &&
                            _ok_up &&
                            _ok_NTFS)
                        {
                            RaiseErrorReportedEvent("収集を完了したので捜索を中断します。");
                            Abort();
                        }
                    }
                }
                catch (Exception ex)
                {
                    RaiseErrorReportedEvent(sourceFile, string.Format("例外が発生しました。: type=\"{0}\", message=\"{1}\"", ex.GetType(), ex.Message));
                }
            }
        }

        private class Worker
            : ConsoleWorker
        {
            private readonly IReadOnlyCollection<IFileWorker> _workers;

            public Worker(IWorkerCancellable canceller)
                : base(canceller)
            {
                _workers = new[] { new Walker(canceller) }.ToReadOnlyCollection();
            }

            protected override IReadOnlyCollection<IFileWorker> Workers => _workers;

            protected override string FirstMessage => "";

            protected override string NormalCompletionMessage => "正常に終了しました。";

            protected override string CancellationMessage => "中断されました。";

            protected override string CompletionMessageOnError => "エラーにより終了しました。";
        }

        static void Main(string[] args)
        {
            using (var canceller = new ConsoleBreakCancellale())
            {
                new Worker(canceller).Execute(args);
            }
            Console.Beep();
            Console.ReadLine();
        }
    }
}