using System;
using System.IO;

namespace AutoImageTrimmer
{
    class Program
    {
        private class ImageConverter
            : FileWalkerFromMainArgument
        {
            protected override bool IsMatchFile(FileInfo sourceFile)
            {
                return sourceFile.Name.StartsWith("resized_", StringComparison.InvariantCultureIgnoreCase) ? false : true;
            }

            protected override void ActionForFile(FileInfo sourceFile)
            {
                var fileTime = sourceFile.LastWriteTimeUtc;
                var imageFileDirectory = sourceFile.Directory;
                var image = TrimmableBitmap.LoadBitmap(sourceFile.FullName);
                try
                {
                    if (image != null)
                    {
                        var newFilePath = Path.Combine(imageFileDirectory.FullName, "Resized", "Resized_" + GetFileNameFromTimeStamp(fileTime) + ".png");
                        Directory.CreateDirectory(Path.GetDirectoryName(newFilePath));
                        try
                        {
                            image.Trim();
                            image.SaveImage(newFilePath);
                            File.SetLastWriteTimeUtc(newFilePath, fileTime);
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                }
                finally
                {
                    if (image != null)
                        image.Dispose();
                }
            }
        }

        private static object _lockObject;
        private static int _progressState;

        static Program()
        {
            _lockObject = new object();
        }

        static void Main(string[] args)
        {
            var walker = new ImageConverter();
            walker.ProgressChanged += Walker_ProgressChanged;
            Console.CancelKeyPress += (s, e) =>
            {
                walker.CancelToWalk();
                e.Cancel = true;
            };
            var completed = walker.Walk(args);
            if (completed)
            {
                Console.Write("\r　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　\r");
                Console.WriteLine("正常に終了しました。ENTERキーを押すと終了します。");
            }
            else
            {
                Console.Write("\r　　　　　　　　　　　　　　　　　　　　　　　　　　　　　　\r");
                Console.WriteLine("ユーザからの要求により中断されました。ENTERキーを押すと終了します。");
            }
            Console.ReadLine();
        }

        private static void Walker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var percentageMessage = e.TotalCount > 0 ? string.Format("{0,3}%完了", e.CountOfDone * 100 / e.TotalCount) : "...";
            var progressText = GetProgressText();
            var message = string.Format("{0}　　{1}　　Ctrl+Cを押すと安全に終了できます。", percentageMessage, progressText);
#if DEBUG
            if (_lockObject == null)
                throw new Exception();
#endif
            lock (_lockObject)
            {
                Console.Write(message + "\r");
            }
        }

        private static string GetProgressText()
        {
#if DEBUG
            if (_lockObject == null)
                throw new Exception();
#endif
            lock (_lockObject)
            {
                switch (_progressState)
                {
                    case 0:
                        _progressState = 1;
                        return "■□□□□";
                    case 1:
                        _progressState = 2;
                        return "□■□□□";
                    case 2:
                        _progressState = 3;
                        return "□□■□□";
                    case 3:
                        _progressState = 4;
                        return "□□□■□";
                    case 4:
                        _progressState = 5;
                        return "□□□□■";
                    case 5:
                        _progressState = 6;
                        return "□□□■□";
                    case 6:
                        _progressState = 7;
                        return "□□■□□";
                    default:
                        _progressState = 0;
                        return "□■□□□";
                }
            }
        }
    }
}
