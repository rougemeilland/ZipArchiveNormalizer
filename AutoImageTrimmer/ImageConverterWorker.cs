using System;
using System.Collections.Generic;
using System.IO;
using Utility;
using Utility.FileWorker;

namespace AutoImageTrimmer
{
    class ImageConverterWorker
        : FileWorkerFromMainArgument
    {
        public ImageConverterWorker(IWorkerCancellable canceller)
            : base(canceller, FileWorkerConcurrencyMode.ParallelProcessingForEachDirectory)
        {
        }

        public override string Description => "画像の一括トリミングをします。";

        protected override IFileWorkerActionFileParameter? IsMatchFile(FileInfo sourceFile)
        {
            return
                !sourceFile.Name.StartsWith("resized_", StringComparison.InvariantCultureIgnoreCase)
                ? base.IsMatchFile(sourceFile)
                : null;
        }

        protected override void ActionForFile(FileInfo sourceFile, IFileWorkerActionParameter parameter)
        {
            var index = parameter.FileIndexOnSameDirectory;
            var fileTime = sourceFile.LastWriteTimeUtc;
            var imageFileDirectoryPath = sourceFile.Directory?.FullName;
            if (imageFileDirectoryPath is not null)
            {
                var image = TrimmableBitmap.LoadBitmap(sourceFile.FullName);
                try
                {
                    if (image is not null)
                    {
                        var newFilePath = Path.Combine(imageFileDirectoryPath, "Resized", "Resized_" + GetUniqueFileNameFromTimeStamp(fileTime, index) + ".png");
                        var newFileDirectory = Path.GetDirectoryName(newFilePath);
                        if (newFileDirectory is not null)
                        {
                            Directory.CreateDirectory(newFileDirectory);
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
                }
                finally
                {
                    image?.Dispose();
                }
            }
        }
    }
}
