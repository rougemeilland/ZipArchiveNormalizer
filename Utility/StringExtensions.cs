using System;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;

namespace Utility
{
    public static class StringExtensions
    {
        private class StringEnumerable
            : IEnumerable<char>
        {
            private class Enumerator
                : IEnumerator<char>
            {
                private string _originalString;
                private int _offset;
                private int _index;
                private int _limit;

                public Enumerator(string originalString, int offset, int count)
                {
                    if (offset < 0)
                        throw new ArgumentException();
                    if (count < 0)
                        throw new ArgumentException();
                    if (offset + count > originalString.Length)
                        throw new ArgumentException();
                    _originalString = originalString;
                    _offset = offset;
                    _index = offset - 1;
                    _limit = offset + count;
                }

                public char Current => _index >= _offset && _index < _limit ? _originalString[_index] : throw new InvalidOperationException();

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                    // NOP
                }

                public bool MoveNext()
                {
                    if (_index >= _limit)
                        throw new InvalidOperationException();
                    ++_index;
                    return _index < _limit;
                }

                public void Reset()
                {
                    _index = _offset - 1;
                }
            }

            private string _originalString;
            private int _offset;
            private int _count;

            public StringEnumerable(string originalString, int offset, int count)
            {
                _originalString = originalString;
                _offset = offset;
                _count = count;
            }

            public IEnumerator<char> GetEnumerator()
            {
                return new Enumerator(_originalString, _offset, _count);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public static IEnumerable<FileInfo> EnumerateFilesFromArgument(this IEnumerable<string> args)
        {
            return
                args
                .SelectMany(arg =>
                {
                    var file = TryParseAsFilePath(arg);
                    if (file != null)
                        return new[] { file };
                    var directory = TryParseAsDirectoryPath(arg);
                    if (directory != null)
                        return directory.EnumerateFiles("*", SearchOption.AllDirectories);
                    return new FileInfo[0];
                });
        }

        public static string GetLeadingCommonPart(this string s1, string s2, bool ignoreCase = false)
        {
            if (s1 == null)
                return s2;
            if (s2 == null)
                return s1;
            if (s1.Length == 0 || s2.Length == 0)
                return "";
            if (s1.Length > s2.Length)
            {
                var t = s1;
                s1 = s2;
                s2 = t;
            }
#if DEBUG
            if (s1.Length > s2.Length)
                throw new Exception();
#endif
            var found =
                s1
                .Zip(s2, (c1, c2) => new { c1, c2 })
                .Select((item, index) => new { item.c1, item.c2, index })
                .FirstOrDefault(item => CharacterEqual(item.c1, item.c2, ignoreCase) == false);
            return found != null ? s1.Substring(0, found.index) : s1;
        }

        public static string GetTrailingCommonPart(this string s1, string s2, bool ignoreCase = false)
        {
            if (s1 == null)
                return s2;
            if (s2 == null)
                return s1;
            if (s1.Length == 0 || s2.Length == 0)
                return "";
            if (s1.Length > s2.Length)
            {
                var t = s1;
                s1 = s2;
                s2 = t;
            }
#if DEBUG
            if (s1.Length > s2.Length)
                throw new Exception();
#endif
            var found =
                s1.Reverse()
                .Zip(s2.Reverse(), (c1, c2) => new { c1, c2 })
                .Select((item, index) => new { item.c1, item.c2, index })
                .FirstOrDefault(item => CharacterEqual(item.c1, item.c2, ignoreCase) == false);
            return found != null ? s1.Substring(s1.Length - found.index, found.index) : s1;
        }

        public static bool IsNoneOf(this string s, string s1, string s2, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return !s.IsAnyOf(s1, s2, stringComparison);
        }

        public static bool IsNoneOf(this string s, string s1, string s2, string s3, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return !s.IsAnyOf(s1, s2, s3, stringComparison);
        }

        public static bool IsNoneOf(this string s, string s1, string s2, string s3, string s4, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return !s.IsAnyOf(s1, s2, s3, s4, stringComparison);
        }

        public static bool IsNoneOf(this string s, string s1, string s2, string s3, string s4, string s5, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return !s.IsAnyOf(s1, s2, s3, s4, s5, stringComparison);
        }

        public static bool IsNoneOf(this string s, string s1, string s2, string s3, string s4, string s5, string s6, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return !s.IsAnyOf(s1, s2, s3, s4, s5, s6, stringComparison);
        }

        public static bool IsAnyOf(this string s, string s1, string s2, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return
                string.Equals(s, s1, stringComparison) ||
                string.Equals(s, s2, stringComparison);
        }

        public static bool IsAnyOf(this string s, string s1, string s2, string s3, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return
                string.Equals(s, s1, stringComparison) ||
                string.Equals(s, s2, stringComparison) ||
                string.Equals(s, s3, stringComparison);
        }

        public static bool IsAnyOf(this string s, string s1, string s2, string s3, string s4, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return
                string.Equals(s, s1, stringComparison) ||
                string.Equals(s, s2, stringComparison) ||
                string.Equals(s, s3, stringComparison) ||
                string.Equals(s, s4, stringComparison);
        }

        public static bool IsAnyOf(this string s, string s1, string s2, string s3, string s4, string s5, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return
                string.Equals(s, s1, stringComparison) ||
                string.Equals(s, s2, stringComparison) ||
                string.Equals(s, s3, stringComparison) ||
                string.Equals(s, s4, stringComparison) ||
                string.Equals(s, s5, stringComparison);
        }

        public static bool IsAnyOf(this string s, string s1, string s2, string s3, string s4, string s5, string s6, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return
                string.Equals(s, s1, stringComparison) ||
                string.Equals(s, s2, stringComparison) ||
                string.Equals(s, s3, stringComparison) ||
                string.Equals(s, s4, stringComparison) ||
                string.Equals(s, s5, stringComparison) ||
                string.Equals(s, s6, stringComparison);
        }

        public static IEnumerable<char> GetSequence(this string s)
        {
            return new StringEnumerable(s, 0, s.Length);
        }

        public static IEnumerable<char> GetSequence(this string s, int offset)
        {
            return new StringEnumerable(s, offset, s.Length - offset);
        }

        public static IEnumerable<char> GetSequence(this string s, int offset, int count)
        {
            return new StringEnumerable(s, offset, count);
        }

        public static string GetString(this Encoding encoding, IReadOnlyArray<byte> bytes)
        {
            return encoding.GetString(bytes.ToArray());
        }

        public static IReadOnlyArray<byte> GetReadOnlyBytes(this Encoding encoding, string s)
        {
            return encoding.GetBytes(s).AsReadOnly();
        }

        private static FileInfo TryParseAsFilePath(string path)
        {
            try
            {
                var file = new FileInfo(path);
                if (!file.Exists)
                    return null;
                return file;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static DirectoryInfo TryParseAsDirectoryPath(string path)
        {
            try
            {
                var directory = new DirectoryInfo(path);
                if (!directory.Exists)
                    return null;
                return directory;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool CharacterEqual(char c1, char c2, bool ignoreCase)
        {
            if (ignoreCase)
                return char.ToUpperInvariant(c1) == char.ToUpperInvariant(c2);
            else
                return c1 == c2;
        }
    }
}