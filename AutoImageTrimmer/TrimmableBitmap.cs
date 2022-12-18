using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Utility;

namespace AutoImageTrimmer
{
    class TrimmableBitmap
        : IDisposable
    {
        private bool _isDisposed;
        private Bitmap _image;
        private Int32 _top;
        private Int32 _bottom;
        private Int32 _left;
        private Int32 _right;

        private TrimmableBitmap(Bitmap image)
        {
            _isDisposed = false;
            _image = image;
            ResetImageMetrics();
        }

        public static TrimmableBitmap? LoadBitmap(string imageFilePath)
        {
            try
            {
                var imageFile = new FileInfo(imageFilePath);
                if (!imageFile.Exists)
                    return null;
                if (imageFile.Length == 0)
                    return null;
                if (imageFile.Extension.ToLowerInvariant().IsNoneOf(".bmp", ".jpg", ".jpeg", ".png"))
                    return null;
                using var imageFileStream = new FileStream(imageFile.FullName, FileMode.Open, FileAccess.Read);
                try
                {
                    var bitmap = new Bitmap(imageFileStream);
                    return new TrimmableBitmap(bitmap);
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }
            catch (IOException)
            {
                return null;
            }
        }

        public bool Trim()
        {
            var top = _top;
            var bottom = _bottom;
            var left = _left;
            var right = _right;

            var colorLeftTop = _image.GetPixel(left, top);
            var colorLeftBottom = _image.GetPixel(left, bottom);
            var colorRightTop = _image.GetPixel(right, top);
            var colorRightBottom = _image.GetPixel(right, bottom);
            if (colorLeftTop != colorLeftBottom
                || colorLeftTop != colorRightTop
                || colorLeftTop != colorRightBottom)
            {
                return false;
            }
            var backgroundColor = colorLeftTop;
            while (left <= right && IsMatchVerticalPixels(backgroundColor, left, top, bottom))
                ++left;
            while (left <= right && IsMatchVerticalPixels(backgroundColor, right, top, bottom))
                --right;
            while (top <= bottom && IsMatchHorizontalPixels(backgroundColor, top, left, right))
                ++top;
            while (top <= bottom && IsMatchHorizontalPixels(backgroundColor, bottom, left, right))
                --bottom;
            try
            {
                var newImage = _image.Clone(new Rectangle(left, top, right - left + 1, bottom - top + 1), _image.PixelFormat);
                if (_image is not null)
                {
                    _image.Dispose();
                    _image = newImage;
                }
            }
            catch (OutOfMemoryException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            ResetImageMetrics();
            return true;
        }

        public void SaveImage(string newImageFilePath)
        {
            using var newImageStream = new FileStream(newImageFilePath, FileMode.CreateNew);
            _image.Save(newImageStream, ImageFormat.Png);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                    _image.Dispose();
                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private bool IsMatchHorizontalPixels(Color backgroundColor, Int32 y, Int32 left, Int32 right)
        {
            return
                Enumerable.Range(left, right - left + 1)
                .All(x => _image.GetPixel(x, y) == backgroundColor);
        }

        private bool IsMatchVerticalPixels(Color backgroundColor, Int32 x, Int32 top, Int32 bottom)
        {
            return
                Enumerable.Range(top, bottom - top + 1)
                .All(y => _image.GetPixel(x, y) == backgroundColor);
        }

        private void ResetImageMetrics()
        {
            _top = 0;
            _bottom = _image.Height - 1;
            _left = 0;
            _right = _image.Width - 1;
        }
    }
}