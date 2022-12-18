using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Utility
{
    public static class StringExtensions
    {
        #region ChunkAsString

        public static IEnumerable<string> ChunkAsString(this IEnumerable<char> source, Int32 count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            var sb = new StringBuilder();
            foreach (var c in source)
            {
                sb.Append(c);
                if (sb.Length >= count)
                {
                    yield return sb.ToString();
                    sb.Clear();
                }
            }
        }

        #endregion

        #region Slice

        public static ReadOnlyMemory<char> Slice(this string sourceString, Int32 offset)
        {
            if (sourceString is null)
                throw new ArgumentNullException(nameof(sourceString));
            if (!offset.IsBetween(0, sourceString.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));

            return (ReadOnlyMemory<char>)sourceString[offset..].ToCharArray();
        }

        public static ReadOnlyMemory<char> Slice(this string sourceString, UInt32 offset) =>
            sourceString.Slice(checked((Int32)offset));

        public static ReadOnlyMemory<char> Slice(this string sourceString, Range range)
        {
            if (sourceString is null)
                throw new ArgumentNullException(nameof(sourceString));
            var sourceArray = sourceString.ToCharArray();
            var (isOk, offset, count) = sourceArray.GetOffsetAndLength(range);
            if (!isOk)
                throw new ArgumentOutOfRangeException(nameof(range));

            return (ReadOnlyMemory<char>)sourceString.Substring(offset, count).ToCharArray();
        }

        public static ReadOnlyMemory<char> Slice(this string sourceString, Int32 offset, Int32 count)
        {
            if (sourceString is null)
                throw new ArgumentNullException(nameof(sourceString));
            var sourceArray = sourceString.ToCharArray();
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            checked
            {
                if (count + offset > sourceArray.Length)
                    throw new ArgumentException($"The specified range ({nameof(offset)} and {nameof(count)}) is not within the {nameof(sourceArray)}.");
            }

            return (ReadOnlyMemory<char>)sourceString.Substring(offset, count).ToCharArray();
        }

        public static ReadOnlyMemory<char> Slice(this string sourceString, UInt32 offset, UInt32 count) =>
            sourceString.Slice(checked((Int32)offset), checked((Int32)count));

        #endregion

        public static IEnumerable<FileInfo> EnumerateFilesFromArgument(this IEnumerable<string> args)
        {
            if (args is null)
                throw new ArgumentNullException(nameof(args));

            return
                args
                .SelectMany(arg =>
                {
                    var file = TryParseAsFilePath(arg);
                    if (file is not null)
                        return new[] { file };
                    var directory = TryParseAsDirectoryPath(arg);
                    if (directory is not null)
                        return directory.EnumerateFiles("*", SearchOption.AllDirectories);
                    return Array.Empty<FileInfo>();
                });
        }

        public static string? GetLeadingCommonPart(this string? s1, string? s2, bool ignoreCase = false)
        {
            if (s1 is null)
                return s2;
            if (s2 is null)
                return s1;
            if (s1.Length == 0 || s2.Length == 0)
                return "";
            if (s1.Length > s2.Length)
                (s2, s1) = (s1, s2);
#if DEBUG
            if (s1.Length > s2.Length)
                throw new Exception();
#endif
            var found =
                s1
                .Zip(s2, (c1, c2) => new { c1, c2 })
                .Select((item, index) => new { item.c1, item.c2, index })
                .FirstOrDefault(item => !CharacterEqual(item.c1, item.c2, ignoreCase));
            return found is not null ? s1[..found.index] : s1;
        }

        public static string? GetTrailingCommonPart(this string? s1, string? s2, bool ignoreCase = false)
        {
            if (s1 is null)
                return s2;
            if (s2 is null)
                return s1;
            if (s1.Length == 0 || s2.Length == 0)
                return "";
            if (s1.Length > s2.Length)
                (s2, s1) = (s1, s2);
#if DEBUG
            if (s1.Length > s2.Length)
                throw new Exception();
#endif
            var found =
                s1.Reverse()
                .Zip(s2.Reverse(), (c1, c2) => new { c1, c2 })
                .Select((item, index) => new { item.c1, item.c2, index })
                .FirstOrDefault(item => !CharacterEqual(item.c1, item.c2, ignoreCase));
            return found is not null ? s1.Substring(s1.Length - found.index, found.index) : s1;
        }

        #region IsNoneOf

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNoneOf(this string s, string s1, string s2, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return
                !string.Equals(s, s1, stringComparison)
                && !string.Equals(s, s2, stringComparison);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNoneOf(this string s, string s1, string s2, string s3, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return
                !string.Equals(s, s1, stringComparison)
                && !string.Equals(s, s2, stringComparison)
                && !string.Equals(s, s3, stringComparison);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNoneOf(this string s, string s1, string s2, string s3, string s4, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return
                !string.Equals(s, s1, stringComparison)
                && !string.Equals(s, s2, stringComparison)
                && !string.Equals(s, s3, stringComparison)
                && !string.Equals(s, s4, stringComparison);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNoneOf(this string s, string s1, string s2, string s3, string s4, string s5, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return
                !string.Equals(s, s1, stringComparison)
                && !string.Equals(s, s2, stringComparison)
                && !string.Equals(s, s3, stringComparison)
                && !string.Equals(s, s4, stringComparison)
                && !string.Equals(s, s5, stringComparison);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNoneOf(this string s, string s1, string s2, string s3, string s4, string s5, string s6, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return
                !string.Equals(s, s1, stringComparison)
                && !string.Equals(s, s2, stringComparison)
                && !string.Equals(s, s3, stringComparison)
                && !string.Equals(s, s4, stringComparison)
                && !string.Equals(s, s5, stringComparison)
                && !string.Equals(s, s6, stringComparison);
        }

        #endregion

        #region IsAnyOf

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAnyOf(this string s, string s1, string s2, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return
                string.Equals(s, s1, stringComparison)
                || string.Equals(s, s2, stringComparison);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAnyOf(this string s, string s1, string s2, string s3, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return
                string.Equals(s, s1, stringComparison)
                || string.Equals(s, s2, stringComparison)
                || string.Equals(s, s3, stringComparison);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAnyOf(this string s, string s1, string s2, string s3, string s4, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return
                string.Equals(s, s1, stringComparison)
                || string.Equals(s, s2, stringComparison)
                || string.Equals(s, s3, stringComparison)
                || string.Equals(s, s4, stringComparison);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAnyOf(this string s, string s1, string s2, string s3, string s4, string s5, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return
                string.Equals(s, s1, stringComparison)
                || string.Equals(s, s2, stringComparison)
                || string.Equals(s, s3, stringComparison)
                || string.Equals(s, s4, stringComparison)
                || string.Equals(s, s5, stringComparison);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAnyOf(this string s, string s1, string s2, string s3, string s4, string s5, string s6, StringComparison stringComparison = StringComparison.Ordinal)
        {
            return
                string.Equals(s, s1, stringComparison)
                || string.Equals(s, s2, stringComparison)
                || string.Equals(s, s3, stringComparison)
                || string.Equals(s, s4, stringComparison)
                || string.Equals(s, s5, stringComparison)
                || string.Equals(s, s6, stringComparison);
        }

        #endregion

        public static string GetString(this Encoding encoding, ReadOnlyMemory<byte> bytes)
        {
            if (encoding is null)
                throw new ArgumentNullException(nameof(encoding));

            return encoding.GetString(bytes.Span);
        }

        public static ReadOnlyMemory<byte> GetReadOnlyBytes(this Encoding encoding, string s)
        {
            if (encoding is null)
                throw new ArgumentNullException(nameof(encoding));
            if (s is null)
                throw new ArgumentNullException(nameof(s));

            return encoding.GetBytes(s).AsReadOnly();
        }

        private static FileInfo? TryParseAsFilePath(string path)
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

        private static DirectoryInfo? TryParseAsDirectoryPath(string path)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CharacterEqual(char c1, char c2, bool ignoreCase)
        {
            if (ignoreCase)
                return char.ToUpperInvariant(c1) == char.ToUpperInvariant(c2);
            else
                return c1 == c2;
        }
    }
}
