﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Utility;
using Utility.FileWorker;


namespace ImageFileRenumber
{
    class ImageFileNameRenumberWorker
        : FileWorkerFromMainArgument
    {
        private enum RenumberingMode
        {
            RenumberFromSpecifiedNumber,
            OnlyAdjustPageNumberWidth,
        }

        private class DirectoryParameter
            : IFileWorkerActionDirectoryParameter
        {
            public DirectoryParameter(RenumberingMode mode, string prefix, string suffix, int pageNumberWidth, int firstPageNumber)
            {
                Mode = mode;
                FileNamePrefix = prefix;
                FileNameSuffix = suffix;
                PagenNumberWidth = pageNumberWidth;
                FirstPageNumber = firstPageNumber;
            }

            public RenumberingMode Mode { get; }
            public string FileNamePrefix { get; }
            public string FileNameSuffix { get; }
            public int PagenNumberWidth { get; }
            public int FirstPageNumber { get; }

            public string GetNewFileName(string pageNumberText, int index, string extension)
            {
                if (_digitsPattern.IsMatch(pageNumberText) == false)
                    throw new ArgumentException();
                switch (Mode)
                {
                    case RenumberingMode.RenumberFromSpecifiedNumber:
                        return
                            FileNamePrefix +
                            (FirstPageNumber + index).ToString().PadLeft(PagenNumberWidth, '0') +
                            FileNameSuffix + extension;
                    case RenumberingMode.OnlyAdjustPageNumberWidth:
                        return
                            FileNamePrefix +
                            pageNumberText.PadLeft(PagenNumberWidth, '0') +
                            FileNameSuffix +
                            extension;
                    default:
                        throw new Exception();
                }
            }
        }

        private static Regex _digitsPattern;
        private static Regex _startsWithDigitsPattern;
        private static Regex _endsWithDigitsPattern;
        private static IComparer<string> _fileNameComparer;

        static ImageFileNameRenumberWorker()
        {
            _digitsPattern = new Regex(@"^[0-9]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _startsWithDigitsPattern = new Regex(@"^[0-9]*(?<suffix>.*?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _endsWithDigitsPattern = new Regex(@"^(?<prefix>.*?)[0-9]*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _fileNameComparer = new FilePathNameComparer(FilePathNameComparerrOption.ConsiderSequenceOfDigitsAsNumber);
        }

        public ImageFileNameRenumberWorker(IWorkerCancellable canceller)
            : base(canceller, FileWorkerConcurrencyMode.ParallelProcessingForEachDirectory)
        {
        }

        public override string Description => "画像ファイルの自動変名を行います。";

        protected override IFileWorkerActionFileParameter IsMatchFile(FileInfo sourceFile)
        {
            // いずれかの階層のディレクトリ名またはファイル名が '.' で始まるパス名は対象外とする
            // 拡張子が ".jpg", ".png", ".bmp" のいずれかのファイルのみを対象とする
            return
                sourceFile.FullName.Contains(@"\.") == false &&
                sourceFile.Extension.IsAnyOf(".jpg", ".png", ".bmp", StringComparison.InvariantCultureIgnoreCase)
                ? DefaultFileParameter
                : null;
        }

        protected override IFileWorkerActionDirectoryParameter IsMatchDirectory(DirectoryInfo directory, IEnumerable<string> fileNames)
        {
            // fileNames が空または1つしかない場合は自動変名は行わない
            if (fileNames.None() || fileNames.IsSingle())
                return null;

            // 全てのファイルの拡張子を除くファイル名部分に共通する、先頭の文字列と最後の文字列を抽出する
            var fileItems =
                fileNames
                .Select(fileName => new
                {
                    currentFileName = fileName,
                    currentFileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName),
                })
                .ToList();

            var prefix =
                fileItems
                .Select(fileItem => fileItem.currentFileNameWithoutExtension)
                .Aggregate((string)null, (name1, name2) => name1.GetLeadingCommonPart(name2, true));
            prefix = _endsWithDigitsPattern.Match(prefix).Groups["prefix"].Value;

            var suffix =
                fileItems
                .Select(fileItem => fileItem.currentFileNameWithoutExtension)
                .Aggregate((string)null, (name1, name2) => name1.GetTrailingCommonPart(name2, true));
            suffix = _startsWithDigitsPattern.Match(prefix).Groups["suffix"].Value;

            // ファイルごとに異なる中央部分の文字列を抽出する
            var middleParts =
                fileItems
                .Select(fileItem => new
                {
                    fileItem.currentFileName,
                    middlePart =
                        fileItem.currentFileNameWithoutExtension.Substring(
                            prefix.Length,
                            fileItem.currentFileNameWithoutExtension.Length - prefix.Length - suffix.Length),
                })
                .ToList();

            // ファイルごとに異なる中央部分の文字列が数字以外の文字をファイルが一つでも存在する場合は、自動変名を行わない
            if (middleParts
                .NotAll(item => _digitsPattern.IsMatch(item.middlePart)))
                return null;

            // この時点で、全てのファイルの中央部分の文字列は数字のみから構成されている

            // 中央部分の数字列をページの順序を表す番号とみなす
            var pageNumbers =
                middleParts
                .Select(item => new
                {
                    item.currentFileName,
                    pageNumber = BigInteger.Parse(item.middlePart, NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat),
                })
                .ToReadOnlyCollection();

            // 重複する番号が存在する場合は順序を一意に決定できないので、自動変名は行わない
            if (pageNumbers.GroupBy(item => item.pageNumber).Any(g => g.Count() > 1))
                return null;

            // 最初に想定するモードはページ桁を合わせることのみ
            var mode = RenumberingMode.OnlyAdjustPageNumberWidth;

            // 最初と最後のページ番号を求める
            var firstPageNumber = pageNumbers.Min(item => item.pageNumber);
            var lastPageNumber = pageNumbers.Max(item => item.pageNumber);

            if (lastPageNumber - firstPageNumber + 1 > middleParts.Count * 3)
            {
                // lastPageNumber - firstPageNumber + 1 (総ページ数?) が 実際のファイルの数の 3 倍を超えている場合は
                // おそらく単純なページ番号のナンバリングではないので、1 から始まる番号に振りなおす。
                firstPageNumber = 1;
                lastPageNumber = firstPageNumber + pageNumbers.Count - 1;
                mode = RenumberingMode.RenumberFromSpecifiedNumber;
            }
            else if (firstPageNumber >= 1000)
            {
                // 最初のページ番号だと思われる数値の最小値がいきなり 1000 以上の場合は
                // おそらく単純なページ番号のナンバリングではないので、1 から始まる番号に振りなおす。
                firstPageNumber = 1;
                lastPageNumber = firstPageNumber + pageNumbers.Count - 1;
                mode = RenumberingMode.RenumberFromSpecifiedNumber;
            }
            else
            {
                // 
                // MOP
            }

            // 最大のページ番号を表現できるようにページ番号の桁数を求める
            var pageNumberWidth = lastPageNumber.ToString().Length;

#if DEBUG
            if (firstPageNumber > int.MaxValue)
                throw new Exception();
#endif
            // ディレクトリパラメタを構築する。
            var directoryParameter = new DirectoryParameter(mode, prefix, suffix, pageNumberWidth, (int)firstPageNumber);

            // 全てのファイルについて、新ファイル名の案を作成する
            var newFileNames =
                pageNumbers
                .OrderBy(item => item.pageNumber)
                .Select((item, index) => new
                {
                    item.currentFileName,
                    newFileName = directoryParameter.GetNewFileName(item.pageNumber.ToString(), index, Path.GetExtension(item.currentFileName)),
                })
                .Select((item, index) => new
                {
                    item.currentFileName,
                    item.newFileName,
                    newFileNPath = Path.Combine(directory.FullName, item.newFileName)
                });

            // 全てのファイルについて、新ファイル名が元のファイル名と一致せずかつ新ファイル名が既存のファイルに存在するものが一つでもあれば、自動変名は行わない
            var found =
                newFileNames
                .Where(item =>
                    string.Equals(item.currentFileName, item.newFileName, StringComparison.InvariantCultureIgnoreCase) == false &&
                    File.Exists(item.newFileNPath))
                .FirstOrDefault();
            if (found != null)
                return null;
            return directoryParameter;
        }

        protected override IComparer<FileInfo> FileComparer =>
            new CustomizableComparer<FileInfo>(
                (file1, file2) => _fileNameComparer.Compare(file1.FullName, file2.FullName),
                (file1, file2) => StringComparer.InvariantCultureIgnoreCase.Equals(file1.FullName, file2.FullName),
                file => file.GetHashCode());

        protected override void ActionForFile(FileInfo sourceFile, IFileWorkerActionParameter parameter)
        {
            var directoryParameter = (DirectoryParameter)parameter.DirectoryParameter;
            var newFilePath =
                Path.Combine(
                    sourceFile.DirectoryName,
                    directoryParameter.FileNamePrefix + (directoryParameter.FirstPageNumber + parameter.FileIndexOnSameDirectory).ToString().PadLeft(directoryParameter.PagenNumberWidth, '0') + directoryParameter.FileNameSuffix + sourceFile.Extension);
            if (!string.Equals(sourceFile.FullName, newFilePath) && File.Exists(newFilePath) == false)
            {
                // MoveTo メソッドは FileInfo オブジェクトを改変してしまうため、
                // 複製してから呼び出している
                new FileInfo(sourceFile.FullName).MoveTo(newFilePath);
                AddToDestinationFiles(new FileInfo(newFilePath));
                IncrementChangedFileCount();
            }
            else
                AddToDestinationFiles(sourceFile);
        }
    }
}