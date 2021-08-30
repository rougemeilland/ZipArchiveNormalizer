using System;
using System.Drawing;
using System.IO;

namespace ImageUtility
{
    public interface IImageFileSize
    {
        FileInfo ImageFile { get; }
        UInt32 ImageFileCrc { get; }
        Size ImageSize { get; }
    }
}