using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace ZipArchiveNormalizer
{
    class ZipArchiveEntryFilePathNameComparer
        : IComparer<string>
    {
        private static IFormatProvider _invariantCultureFormatProvider;
        private Regex _prefixPattern;
        private Regex _digitsPattern;
        private Regex _delimiterPattern;
        private ZipArchiveEntryFilePathNameComparerrOption _option;

        static ZipArchiveEntryFilePathNameComparer()
        {
            _invariantCultureFormatProvider = CultureInfo.InvariantCulture.NumberFormat;
        }

        public ZipArchiveEntryFilePathNameComparer(ZipArchiveEntryFilePathNameComparerrOption option)
        {
            _option = option;
            if ((option & ZipArchiveEntryFilePathNameComparerrOption.ConsiderSequenceOfDigitsAsNumber) != ZipArchiveEntryFilePathNameComparerrOption.None)
            {
                _prefixPattern = new Regex(@"\G(?<prefix>[^0-9/\\]+)", RegexOptions.Compiled);
                _digitsPattern = new Regex(@"\G(?<digits>[0-9]+)", RegexOptions.Compiled);
                _delimiterPattern = new Regex(@"\G(?<delimiter>[/\\])", RegexOptions.Compiled);
            }
            else
            {
                _prefixPattern = new Regex(@"\G(?<prefix>[^/\\]+)", RegexOptions.Compiled);
                _digitsPattern = null;
                _delimiterPattern = new Regex(@"\G(?<delimiter>[/\\])", RegexOptions.Compiled);
            }
        }

        private int Compare(string x, string y)
        {
#if DEBUG
            if (x == null)
                throw new Exception();
            if (y == null)
                throw new Exception();
#endif
            var x_index = 0;
            var y_index = 0;
            while (x_index < x.Length && y_index < y.Length)
            {
                var x_digits_match = _digitsPattern != null ? _digitsPattern.Match(x, x_index) : null;
                if (x_digits_match != null && x_digits_match.Success)
                {
                    // x の部分文字列が数字で始まっていた場合
                    var y_digits_match = _digitsPattern != null ? _digitsPattern.Match(y, y_index) : null;
                    if (y_digits_match != null && y_digits_match.Success)
                    {
                        // y の部分文字列が数字で始まる場合
                        var x_digits = x_digits_match.Groups["digits"].Value;
                        var y_digits = y_digits_match.Groups["digits"].Value;
                        var x_digits_value = BigInteger.Parse(x_digits, NumberStyles.None, _invariantCultureFormatProvider);
                        var y_digits_value = BigInteger.Parse(y_digits, NumberStyles.None, _invariantCultureFormatProvider);
                        int c = x_digits_value.CompareTo(y_digits_value);
                        if (c != 0)
                            return c;
                        x_index += x_digits.Length;
                        y_index += y_digits.Length;
                    }
                    else
                    {
                        // y の部分文字列が非数字で始まっていた場合
                        break;
                    }
                }
                else
                {
                    // x の部分文字列が非数字で始まっていた場合
                    var x_delimiter_match = _delimiterPattern.Match(x, x_index);
                    if (x_delimiter_match.Success)
                    {
                        // x の部分文字列がデリミタで始まっていた場合
                        var y_delimiter_match = _delimiterPattern.Match(y, y_index);
                        if (y_delimiter_match.Success)
                        {
                            // y の部分文字列がデリミタで始まる場合
                            var x_delimiter = x_delimiter_match.Groups["delimiter"].Value;
                            var y_delimiter = y_delimiter_match.Groups["delimiter"].Value;
                            x_index += x_delimiter.Length;
                            y_index += y_delimiter.Length;
                        }
                        else
                        {
                            // y の部分文字列が非デリミタで始まっていた場合
                            break;
                        }
                    }
                    else
                    {
                        // x の部分文字列が非数字非デリミタで始まっていた場合
                        var x_prefix_match = _prefixPattern.Match(x, x_index);
#if DEBUG
                        if (!x_prefix_match.Success)
                            throw new Exception();
#endif
                        // x の部分文字列が非数字非デリミタで始まっていた場合
                        var x_prefix = x_prefix_match.Groups["prefix"].Value;
                        int length = x_prefix.Length;
                        var c = CompareSubstring(x, y, x_index, y_index, ref length);
                        if (c != 0)
                            return c;
                        x_index += length;
                        y_index += length;
                    }
                }
            }
            // 文字列の残りの部分を単純に文字列として比較してその結果を返す
            return CompareSubstring(x, y, x_index, y_index);
        }

        private static int CompareSubstring(string x, string y, int x_index, int y_index)
        {
            int length = int.MaxValue;
            return CompareSubstring(x, y, x_index, y_index, ref length);
        }

        private static int CompareSubstring(string x, string y, int x_index, int y_index, ref int length)
        {
            if (length > x.Length - x_index)
                length = x.Length - x_index;
            if (length > y.Length - y_index)
                length = y.Length - y_index;
            return string.Compare(x, x_index, y, y_index, length, StringComparison.OrdinalIgnoreCase);
        }

        int IComparer<string>.Compare(string x, string y)
        {
            if (x == null)
                return y == null ? 0 : -1;
            else
                return y == null ? 1 : Compare(x, y);
        }
    }

}
