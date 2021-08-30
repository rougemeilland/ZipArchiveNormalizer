using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

namespace ImageUtility
{
    public class ImageFileDirectorySummary
    {
        private static IDictionary<UInt32, object> _allowedImageFileCrcs;

        static ImageFileDirectorySummary()
        {
            _allowedImageFileCrcs = new[]
            {
                0xCDB43943U,
                0x692D8095U,
                0xB48D63F8U,
                0x6057F760U,
                0xDCCCBCF9U,
                0x37F9F844U,
                0x73118FDBU,
            }
            .ToDictionary(crc => crc, crc => new object());
        }

        public ImageFileDirectorySummary(DirectoryInfo directory, IEnumerable<IImageFileSize> imageFiles)
        {
            Directory = directory;
            ImageFileOfMinimumWidth =
                imageFiles
                .Where(imageFile => !_allowedImageFileCrcs.ContainsKey(imageFile.ImageFileCrc))
                .OrderBy(imageFile => imageFile.ImageSize.Width)
                .First();
            ImageFileOfMaximumWidth =
                imageFiles
                .Where(imageFile => !_allowedImageFileCrcs.ContainsKey(imageFile.ImageFileCrc))
                .OrderByDescending(imageFile => imageFile.ImageSize.Width)
                .First();
            ImageFileOfMinimumHeight =
                imageFiles
                .Where(imageFile => !_allowedImageFileCrcs.ContainsKey(imageFile.ImageFileCrc))
                .OrderBy(imageFile => imageFile.ImageSize.Height)
                .First();
            ImageFileOfMaximumHeight =
                imageFiles
                .Where(imageFile => !_allowedImageFileCrcs.ContainsKey(imageFile.ImageFileCrc))
                .OrderByDescending(imageFile => imageFile.ImageSize.Height)
                .First();
        }

        public DirectoryInfo Directory { get; }
        public IImageFileSize ImageFileOfMinimumWidth { get; }
        public IImageFileSize ImageFileOfMaximumWidth { get; }
        public IImageFileSize ImageFileOfMinimumHeight { get; }
        public IImageFileSize ImageFileOfMaximumHeight { get; }
    }
}