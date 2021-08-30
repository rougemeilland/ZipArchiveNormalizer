using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Utility
{
    public static class StringExtensions
    {
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

        public static bool IsNoneOf(this string s, string s1, string s2, StringComparison stringComparison = StringComparison.CurrentCulture)
        {
            return !s.IsAnyOf(s1, s2, stringComparison);
        }

        public static bool IsNoneOf(this string s, string s1, string s2, string s3, StringComparison stringComparison = StringComparison.CurrentCulture)
        {
            return !s.IsAnyOf(s1, s2, s3, stringComparison);
        }

        public static bool IsNoneOf(this string s, string s1, string s2, string s3, string s4, StringComparison stringComparison = StringComparison.CurrentCulture)
        {
            return !s.IsAnyOf(s1, s2, s3, s4, stringComparison);
        }

        public static bool IsAnyOf(this string s, string s1, string s2, StringComparison stringComparison = StringComparison.CurrentCulture)
        {
            return
                string.Equals(s, s1, stringComparison) ||
                string.Equals(s, s2, stringComparison);
        }

        public static bool IsAnyOf(this string s, string s1, string s2, string s3, StringComparison stringComparison = StringComparison.CurrentCulture)
        {
            return
                string.Equals(s, s1, stringComparison) ||
                string.Equals(s, s2, stringComparison) ||
                string.Equals(s, s3, stringComparison);
        }

        public static bool IsAnyOf(this string s, string s1, string s2, string s3, string s4, StringComparison stringComparison = StringComparison.CurrentCulture)
        {
            return
                string.Equals(s, s1, stringComparison) ||
                string.Equals(s, s2, stringComparison) ||
                string.Equals(s, s3, stringComparison) ||
                string.Equals(s, s4, stringComparison);
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
                return char.ToLowerInvariant(c1) == char.ToLowerInvariant(c2);
            else
                return c1 == c2;
        }
    }
}