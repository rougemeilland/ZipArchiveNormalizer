using System.Drawing;
using System.IO;

namespace ImageUtility
{
    public static class FileExtensions
    {

        public static Size GetImageSize(this FileInfo imageFile)
        {
            using (var imageFileStream = new FileStream(imageFile.FullName, FileMode.Open, FileAccess.Read))
            using (var bitmap = new Bitmap(imageFileStream))
            {
                return bitmap.Size;
            }
        }
    }
}