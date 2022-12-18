using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Utility;
using Utility.FileWorker;
using Utility.IO;

namespace ZipArchiveNormalizer.Phase5
{
    class Phase5Worker
        : FileWorkerFromMainArgument, IPhaseWorker
    {
        private const string _epubMediaType = "application/epub+zip";

        private static readonly Regex _fileNameReplacePattern;

        private readonly Func<FileInfo, bool> _isBadFileSelecter;

        public event EventHandler<BadFileFoundEventArgs>? BadFileFound;

        static Phase5Worker()
        {
            _fileNameReplacePattern = new Regex("(?<rep>([！？]+)|[　＃＄％＆’（）＋，‐．０-９；＝＠Ａ-Ｚ［］＾＿‘ａ-ｚ｛｝~])", RegexOptions.Compiled);
        }

        public Phase5Worker(IWorkerCancellable canceller, Func<FileInfo, bool> isBadFileSelecter)
            : base(canceller, FileWorkerConcurrencyMode.ParallelProcessingForEachFile)
        {
            _isBadFileSelecter = isBadFileSelecter;
        }

        public override string Description => "ファイル名の単純化を試みます。";

        protected override IFileWorkerActionFileParameter? IsMatchFile(FileInfo sourceFile)
        {
            // 拡張子が ".zip", ".epub", ".pdf" のいずれかのファイルのみを対象とする
            return
                !_isBadFileSelecter(sourceFile) &&
                sourceFile.Extension.IsAnyOf(".zip", ".epub", ".pdf", StringComparison.OrdinalIgnoreCase)
                ? base.IsMatchFile(sourceFile)
                : null;
        }

        protected override void ActionForFile(FileInfo sourceFile, IFileWorkerActionParameter parameter)
        {
            var extension = DetermineNewArchiveFileExtension(sourceFile);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFile.Name);
            var newFileNameWithoutExtension = _fileNameReplacePattern.Replace(fileNameWithoutExtension, FileNameReplacer);
            if (sourceFile.IsAozoraBunko() && !sourceFile.Name.StartsWith(".", StringComparison.OrdinalIgnoreCase))
                fileNameWithoutExtension = "." + fileNameWithoutExtension;
            var zipArchiveFileDirectory = sourceFile.Directory;
            var newArchiveFile = sourceFile.RenameFile(newFileNameWithoutExtension + extension).File;
            if (!string.Equals(newArchiveFile.FullName, sourceFile.FullName, StringComparison.OrdinalIgnoreCase))
            {
                RaiseInformationReportedEvent(sourceFile, string.Format("アーカイブファイルを変名しました。: \"{0}\"", newArchiveFile.Name));
                IncrementChangedFileCount();
            }
            AddToDestinationFiles(newArchiveFile);
        }

        private static string DetermineNewArchiveFileExtension(FileInfo sourceFile)
        {
            if (sourceFile.Extension.IsNoneOf(".zip", ".epub", StringComparison.OrdinalIgnoreCase))
                return sourceFile.Extension;
            using var zipFile = new ZipFile(sourceFile.FullName);
            var entry = zipFile.GetEntry("mimetype");
            if (entry is null)
                return sourceFile.Extension;
            using var inputStream = zipFile.GetInputStream(entry).AsInputByteStream();
            var text = Encoding.ASCII.GetString(inputStream.ReadAllBytes());
            return text == _epubMediaType ? ".epub" : sourceFile.Extension;
        }

        private static string FileNameReplacer(Match match)
        {
#if DEBUG
            if (!match.Success)
                throw new Exception();
#endif
            var sourceText = match.Groups["rep"].Value;


            // '！' と '？' が連続している場合はどちらも半角文字には置換しない
            if (sourceText.Contains('？'))
                return sourceText;

            // '！' が '？' と連続していない場合は、'！' を全て '!' に置換する
            if (sourceText.Contains('！'))
                return sourceText.Replace("！", "!");

            // マッチした文字列の長さが 1 以外の場合は、元の文字列をそのまま返す (この条件は事実上成立することはない)
            if (sourceText.Length != 1)
                return sourceText;

            // この時点では、マッチした文字の長さは 1 である
            var c = sourceText[0];

            // c が全角英数字なら半角文字に置換する

            if (c.IsBetween('０', '９'))
                return char.ConvertFromUtf32(c - '０' + '0').ToString();
            if (c.IsBetween('Ａ', 'Ｚ'))
                return char.ConvertFromUtf32(c - 'Ａ' + 'A').ToString();
            if (c.IsBetween('ａ', 'ｚ'))
                return char.ConvertFromUtf32(c - 'ａ' + 'a').ToString();

            // c がその他の記号なら半角文字に置換する
            return
                c switch
                {

                    '　' => " ",
                    '＃' => "#",
                    '＄' => "$",
                    '％' => "%",
                    '＆' => "&",
                    '’' => "'",
                    '（' => "(",
                    '）' => ")",
                    '＋' => "+",
                    '，' => ",",
                    '‐' => "-",
                    '．' => ".",
                    '；' => ",",
                    '＝' => "=",
                    '＠' => "@",
                    '［' => "[",
                    '］' => "]",
                    '＾' => "^",
                    '＿' => "_",
                    '‘' => "`",
                    '｛' => "{",
                    '｝' => "}",
                    '~' => "～",
                    // このルートに到達することはないはずだが、もし到達したら元の文字列を置換せずにそのまま返す
                    _ =>
#if DEBUG
                    throw new Exception(),
#else
                    sourceText,
#endif
                };
        }

#pragma warning disable IDE0051 // 使用されていないプライベート メンバーを削除する
        private void RaiseBadFileFoundEvent(FileInfo targetFile)
#pragma warning restore IDE0051 // 使用されていないプライベート メンバーを削除する
        {
            if (BadFileFound is not null)
            {
                BadFileFound(this, new BadFileFoundEventArgs(targetFile));
            }
        }
    }
}