using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Utility;
using Utility.FileWorker;
using Utility.IO;
using Utility.Text;
using ZipUtility;

namespace ZipArchiveNormalizer.Phase6
{
    class Phase6Worker
        : FileWorkerFromMainArgument, IPhaseWorker
    {
        private const Int32 _minimumAozoraBunkoTextCharacters = 1000;

        private static readonly Regex _contentTextFileNamePattern;
        private static readonly Regex _imageFileNamePattern;
        private static readonly Regex _filesNotToBeExtractedForAozoraBunkoPattern;
        private static readonly Regex _aozoraBunkoImageLinkPattern;
        private static readonly Encoding _aozoraBunkoTextFileEncoding;

        private readonly Func<FileInfo, bool> _isBadFileSelecter;

        public event EventHandler<BadFileFoundEventArgs>? BadFileFound;

        static Phase6Worker()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _contentTextFileNamePattern = new Regex(@"\.txt$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _imageFileNamePattern = new Regex(@"\.(png|jpg|bmp)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _filesNotToBeExtractedForAozoraBunkoPattern = new Regex(Settings.Default.FilesNotToBeExtractedForAozoraBunkoPatternText, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _aozoraBunkoImageLinkPattern = new Regex(@"((?<prefix>［(?<type>＃)[^（]*（)(?<imagepath>[^、）]+)(?<suffix>(、[^）]+)?）(入る)?］))|((?<prefix><(?<type>img)(\s+[^ ""'\r\n\t>/=]+(\s*=\s*(([^ ""'=<>`\r\n\t]+)|(""[^""]*"")))?)*\s+src="")(?<imagepath>[^""]+)(?<suffix>""(\s+[^ ""'>/=\r\n\t]+(\s*=\s*(([^ ""'=<>`\r\n\t]+)|(""[^""]*"")))?)*\s*(>|/>)))|((?<prefix><(?<type>img)(\s+[^ ""'\r\n\t>/=]+(\s*=\s*(([^ ""'=<>`\r\n\t]+)|(""[^""]*"")))?)*\s+src=)(?<imagepath>[^ ""'=<>`\r\n\t]+)(?<suffix>(\s+[^ ""'>/=\r\n\t]+(\s*=\s*(([^ ""'=<>`\r\n\t]+)|(""[^""]*"")))?)*\s*(>|/>)))", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _aozoraBunkoTextFileEncoding = Encoding.GetEncoding("shift_jis");
        }

        public Phase6Worker(IWorkerCancellable canceller, Func<FileInfo, bool> isBadFileSelecter)
            : base(canceller, FileWorkerConcurrencyMode.ParallelProcessingForEachFile)
        {
            _isBadFileSelecter = isBadFileSelecter;
        }

        public override string Description => "青空文庫テキスト形式ファイルの配置をします。";

        protected override IFileWorkerActionFileParameter? IsMatchFile(FileInfo sourceFile)
        {
            // 青空文庫テキスト形式のアーカイブファイルのみ対象とする
            return
                !_isBadFileSelecter(sourceFile) &&
                sourceFile.IsAozoraBunko()
                ? base.IsMatchFile(sourceFile)
                : null;
        }

        protected override void ActionForFile(FileInfo sourceFile, IFileWorkerActionParameter parameter)
        {
            using var zipFile = sourceFile.OpenAsZipFile();
            var entries = zipFile.GetEntries();
            var mainContentTextEntries =
                entries
                .Where(entry =>
                    _contentTextFileNamePattern.IsMatch(entry.FullName) &&
                    entry.Size >= _minimumAozoraBunkoTextCharacters &&
                    !_filesNotToBeExtractedForAozoraBunkoPattern.IsMatch(Path.GetFileNameWithoutExtension(entry.FullName)))
                .ToList();
            var imageFileEntries =
                entries
                .Where(entry => _imageFileNamePattern.IsMatch(entry.FullName))
                .ToDictionary(entry => entry.FullName, entry => entry, StringComparer.InvariantCultureIgnoreCase);
            if (mainContentTextEntries.None())
            {
                RaiseErrorReportedEvent(
                    sourceFile,
                    "青空文庫アーカイブファイルに本文テキストファイルが見つかりませんでした。");
                return;
            }
            if (!mainContentTextEntries.IsSingle())
            {
                RaiseWarningReportedEvent(
                    sourceFile,
                    string.Format(
                        "青空文庫アーカイブファイルに本文テキストファイルが複数見つかったので自動的な展開を中断しました。: \"{0}\",  \"{1}\", ...",
                        mainContentTextEntries.First().FullName,
                        mainContentTextEntries.Skip(1).First().FullName));
                return;
            }
            var mainContentTextEntry = mainContentTextEntries.First();
            var localTextFileName = Path.GetFileNameWithoutExtension(sourceFile.Name) + ".txt";
            if (localTextFileName.StartsWith("."))
                localTextFileName = localTextFileName[1..];
#if DEBUG
            if (mainContentTextEntry.LastWriteTimeUtc is not null && mainContentTextEntry.LastWriteTimeUtc.Value.Kind != DateTimeKind.Utc)
                throw new Exception();
            if (mainContentTextEntry.LastAccessTimeUtc is not null && mainContentTextEntry.LastAccessTimeUtc.Value.Kind != DateTimeKind.Utc)
                throw new Exception();
            if (mainContentTextEntry.CreationTimeUtc is not null && mainContentTextEntry.CreationTimeUtc.Value.Kind != DateTimeKind.Utc)
                throw new Exception();
#endif
            var success = false;
            var extracted = false;
            var textFileEntrySubDirectory = Path.GetDirectoryName(mainContentTextEntry.FullName) ?? ".";
            var localTextFilePath =
                    string.IsNullOrEmpty(textFileEntrySubDirectory)
                    ? Path.Combine(sourceFile.DirectoryName ?? ".", localTextFileName)
                    : Path.Combine(sourceFile.DirectoryName ?? ".", textFileEntrySubDirectory, localTextFileName);
            var tempFile =
                new FileInfo(Path.Combine(Path.GetDirectoryName(localTextFilePath) ?? ".", "." + Path.GetFileName(localTextFilePath) + ".temp"));
            try
            {
                tempFile.Directory?.Create();
                tempFile.WriteAllBytes(
                    ReadAozoraBunkoContentEntry(
                        zipFile,
                        mainContentTextEntry,
                        entries,
                        sourceFile.DirectoryName ?? ".",
                        textFileEntrySubDirectory,
                        (imageEntryName, imageFile, newLocalFileCreated) =>
                        {
                            if (newLocalFileCreated)
                                extracted = true;
                            imageFileEntries.Remove(imageEntryName);
                        }));
                var (newContentTextFile, alreadyExistsTextFile) = tempFile.RenameFile(Path.GetFileName(localTextFilePath));
                if (!alreadyExistsTextFile)
                {
                    mainContentTextEntry.SeTimeStampToExtractedFile(newContentTextFile.FullName);
                    extracted = true;
                }
                if (extracted)
                    RaiseInformationReportedEvent(sourceFile, "アーカイブファイルを展開しました。");
                var newSourceFileName = sourceFile.Name;
                if (!newSourceFileName.StartsWith(".", StringComparison.OrdinalIgnoreCase))
                {
                    newSourceFileName = "." + newSourceFileName;
                    UpdateProgress();
                    var newSourceFile = sourceFile.RenameFile(Path.Combine(sourceFile.DirectoryName ?? ".", newSourceFileName)).File;
                    AddToDestinationFiles(newSourceFile);
                    IncrementChangedFileCount();
                }
                else
                {
                    AddToDestinationFiles(sourceFile);
                    if (extracted)
                        IncrementChangedFileCount();
                }
                if (imageFileEntries.Any())
                {
                    RaiseWarningReportedEvent(
                        sourceFile,
                        string.Format(
                            "参照されていないイメージファイルが含まれています。: {{{0}}}",
                            string.Join(
                                ", ",
                                imageFileEntries.Keys
                                .OrderBy(entryName => entryName, StringComparer.InvariantCultureIgnoreCase)
                                .Select(entryName => string.Format("\"{0}\"", entryName)))));
                }
                success = true;
            }
            finally
            {
                if (tempFile.Exists)
                    tempFile.Delete();
                if (!success)
                {
                    RaiseErrorReportedEvent(
                        sourceFile,
                        "アーカイブファイルからエントリの展開に失敗しました。");
                }
            }
        }

        private IEnumerable<byte> ReadAozoraBunkoContentEntry(ZipArchiveFile zipFile, ZipArchiveEntry aozoraBunkoMainContentEntry, ZipArchiveEntryCollection zipArchiveEntries, string archiveDirectoryPath, string textFileEntrySubDirectory, Action<string, FileInfo, bool> actionOnImageExtracted)
        {
            return
                zipFile.GetContentStream(aozoraBunkoMainContentEntry)
                .GetByteSequence()
                .DecodeAsShiftJisChar()
                .ReplaceAozoraBunkoImageTag(
                    imagePath =>
                        AozoraBunkoTextImageLinkReplacer(
                            zipFile,
                            zipArchiveEntries,
                            archiveDirectoryPath,
                            textFileEntrySubDirectory,
                            imagePath,
                            actionOnImageExtracted))
                .EncodeAsShiftJisChar();
        }

        private string AozoraBunkoTextImageLinkReplacer(ZipArchiveFile zipFile, ZipArchiveEntryCollection zipArchiveEntries, string archiveDirectoryPath, string textFileEntrySubDirectory, string imagePath, Action<string, FileInfo, bool> actionOnImageExtracted)
        {
            var imageFileSubdirectory = Path.GetDirectoryName(imagePath);
            var imageFileName = Path.GetFileName(imagePath);
            if (string.IsNullOrEmpty(imageFileSubdirectory))
                imageFileSubdirectory = ".img";
            else if (!imageFileSubdirectory.StartsWith(".", StringComparison.OrdinalIgnoreCase))
                imageFileSubdirectory = "." + imageFileSubdirectory;
            else
            {
                // NOP
            }
            var imageEntryName = string.IsNullOrEmpty(textFileEntrySubDirectory) ? imagePath : Path.Combine(textFileEntrySubDirectory, imagePath).Replace('\\', '/');
            var imageLocalFilePath =
                string.IsNullOrEmpty(textFileEntrySubDirectory)
                ? Path.Combine(archiveDirectoryPath, imageFileSubdirectory, imageFileName)
                : Path.Combine(archiveDirectoryPath, textFileEntrySubDirectory, imageFileSubdirectory, imageFileName);
            var imageTemporaryFile = new FileInfo(Path.Combine(Path.GetDirectoryName(imageLocalFilePath) ?? ".", "." + Path.GetFileName(imageLocalFilePath) + ".temp"));
            try
            {
                if (!ExtractImageFile(zipFile, zipArchiveEntries, imageEntryName, imageTemporaryFile))
                    return imagePath;
                var foundImageArchiveEntry =
                    zipArchiveEntries
                    .Where(entry => string.Equals(entry.FullName, imageEntryName, StringComparison.OrdinalIgnoreCase))
                    .Take(1)
                    .ToArray();
                if (foundImageArchiveEntry.Length <= 0)
                    throw new InternalLogicalErrorException();
#if DEBUG
                var lastWriteTimeUtc = foundImageArchiveEntry[0].LastWriteTimeUtc;
                var lastAccessTimeUtc = foundImageArchiveEntry[0].LastAccessTimeUtc;
                var creationTimeUtc = foundImageArchiveEntry[0].CreationTimeUtc;
                if (lastWriteTimeUtc.HasValue && lastWriteTimeUtc.Value.Kind != DateTimeKind.Utc)
                    throw new Exception();
                if (lastAccessTimeUtc.HasValue && lastAccessTimeUtc.Value.Kind != DateTimeKind.Utc)
                    throw new Exception();
                if (creationTimeUtc.HasValue && creationTimeUtc.Value.Kind != DateTimeKind.Utc)
                    throw new Exception();

#endif
                var (newImageFile, alreadyExistsImageFile) = imageTemporaryFile.RenameFile(imageLocalFilePath);
                if (!alreadyExistsImageFile)
                    foundImageArchiveEntry[0].SeTimeStampToExtractedFile(newImageFile.FullName);
                actionOnImageExtracted(imageEntryName, newImageFile, !alreadyExistsImageFile);
                return Path.Combine(imageFileSubdirectory, Path.GetFileName(newImageFile.FullName)).Replace('\\', '/');
            }
            catch (Exception)
            {
                // 何らかの異常が発生した場合には無置換で返す
                return imagePath;
            }
            finally
            {
                if (imageTemporaryFile.Exists)
                    imageTemporaryFile.Delete();
            }
        }

        private bool ExtractImageFile(ZipArchiveFile zipFile, ZipArchiveEntryCollection zipArchiveEntries, string imageEntryName, FileInfo imageLocalFile)
        {
            try
            {
                var imageEntry = zipArchiveEntries[imageEntryName];
                if (!imageEntry.HasValue)
                    return false;
                imageLocalFile.Directory?.Create();
                using var imageInputStream = zipFile.GetContentStream(imageEntry.Value);
                using var localImageFileStream = imageLocalFile.Create().AsOutputByteStream();
                imageInputStream.CopyTo(localImageFileStream, new Progress<UInt64>(_ => UpdateProgress()));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void RaiseBadFileFoundEvent(FileInfo targetFile)
        {
            if (BadFileFound is not null)
                BadFileFound(this, new BadFileFoundEventArgs(targetFile));
        }
    }
}
