using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace AutoImageTrimmer
{
    class TrimmableBitmap
        : IDisposable
    {
        private bool _isDisposed;
        private Bitmap _image;
        private int _top;
        private int _bottom;
        private int _left;
        private int _right;

        private TrimmableBitmap(Bitmap image)
        {
            _isDisposed = false;
            _image = image;
            ResetImageMetrics();
        }

        public static TrimmableBitmap LoadBitmap(string imageFilePath)
        {
            try
            {
                var imageFile = new FileInfo(imageFilePath);
                if (imageFile.Exists == false)
                    return null;
                if (imageFile.Length == 0)
                    return null;
                if (!new[] { ".bmp", ".jpg", ".png" }.Contains(imageFile.Extension.ToLowerInvariant()))
                    return null;
                using (var imageFileStream = new FileStream(imageFile.FullName, FileMode.Open, FileAccess.Read))
                {
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
            var backgroundColors = new[]
            {
                _image.GetPixel(left, top),
                _image.GetPixel(left, bottom),
                _image.GetPixel(right, top),
                _image.GetPixel(right, bottom),
            }
            .Distinct();
            if (backgroundColors.Skip(1).Any())
                return false;
            var backgroundColor = backgroundColors.Single();
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
                if (_image != null)
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
            using (var newImageStream = new FileStream(newImageFilePath, FileMode.CreateNew))
            {
                _image.Save(newImageStream, ImageFormat.Png);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                }

                if (_image != null)
                {
                    _image.Dispose();
                    _image = null;
                }
                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private bool IsMatchHorizontalPixels(Color backgroundColor, int y, int left, int right)
        {
            return
                Enumerable.Range(left, right - left + 1)
                .All(x => _image.GetPixel(x, y) == backgroundColor);
        }

        private bool IsMatchVerticalPixels(Color backgroundColor, int x, int top, int bottom)
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