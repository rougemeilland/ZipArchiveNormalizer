using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Utility
{
    public class FilePathNameComparer
        : IComparer<string>
    {
        private static IReadOnlyCollection<Tuple<int, Regex>> _contentFileNamePatterns;
        private static Regex _digitsPattern;

        private CultureInfo _culture;
        private FilePathNameComparerrOption _option;

        static FilePathNameComparer()
        {
            _contentFileNamePatterns = new[]
            {
                // 優先すべきファイルのパターンを先に書く
                new Regex(@"^mimetype$", RegexOptions.Compiled | RegexOptions.CultureInvariant),
                new Regex(@"^META-INF([\\/].*)?$", RegexOptions.Compiled | RegexOptions.CultureInvariant),
            }
            .Select((pattern, index) => new Tuple<int, Regex>(index, pattern))
            .ToReadOnlyCollection();
            _digitsPattern = new Regex(@"(?<digits>[0-9]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

        public FilePathNameComparer()
            : this(FilePathNameComparerrOption.IgnoreCase, null)
        {
        }

        public FilePathNameComparer(StringComparison comparisonMethod)
            : this(GetOptionFromStringComparison(comparisonMethod), GetCultureFromStringComparison(comparisonMethod))
        {
        }

        public FilePathNameComparer(FilePathNameComparerrOption option)
            : this(option, null)
        {
        }

        public FilePathNameComparer(FilePathNameComparerrOption option, CultureInfo culture)
        {
            _option = option;
            _culture = culture;
        }

        public int Compare(string x, string y)
        {
            if (x == null)
                return y == null ? 0 : -1;
            else if (y == null)
                return 1;
            else if (_option.HasFlag(FilePathNameComparerrOption.ConsiderContentFile))
            {
                var x_priority = _contentFileNamePatterns.Select(pattern => pattern.Item2.IsMatch(x) ? pattern.Item1 : -1).Where(value => value >= 0).Concat(new[] { int.MaxValue }).First();
                var y_priority = _contentFileNamePatterns.Select(pattern => pattern.Item2.IsMatch(y) ? pattern.Item1 : -1).Where(value => value >= 0).Concat(new[] { int.MaxValue }).First();
                int c;
                if ((c = x_priority.CompareTo(y_priority)) != 0)
                    return c;
            }

            if ((_option & ~(FilePathNameComparerrOption.IgnoreCase | FilePathNameComparerrOption.ConsiderContentFile)) != FilePathNameComparerrOption.None)
                return InternalCompare(x, y);
            else
                return InternalCompareForSimpleString(x, y);
        }

        private int InternalCompare(string x, string y)
        {
            /*
             * InvariantCulture において(そして多分他のCultureでも)、ある文字列の比較結果とそれらの文字列の部分文字列の比較結果が異なるなど、
             * 文字列の比較がよく理解できない挙動をしているため、文字列を分割しつつ比較をするようなことは避けた方がいい模様。
             * 
             * string.Compare("abc", "A010", StringComparison.Ordinal) > 0 (当然)
             * string.Compare("a", "A", StringComparison.Ordinal) > 0 (当然)
             * 
             * string.Compare("abc", "A010", false, CultureInfo.InvariantCulture) > 0 (これは Ordinal と同じなので納得)
             * string.Compare("a", "A", false, CultureInfo.InvariantCulture) < 0 (先頭の文字を取り出して比較しただけなのになぜこうなる!?)
             */

            if (_option.HasFlag(FilePathNameComparerrOption.ConsiderDigitSequenceOfsAsNumber))
            {
                // x と y に出現する連続する数字(先頭の0を除く)の長さの最大値を求める
                // ただし、0 の場合は 1 にする。
                Func<Match, int> digitsSelecter = m => m.Groups["digits"].Value.TrimStart('0').Length;
                var maxDigitsLength =
                    _digitsPattern.Matches(x)
                    .Cast<Match>()
                    .Select(m => digitsSelecter(m))
                    .Concat(
                        _digitsPattern.Matches(y)
                        .Cast<Match>()
                        .Select(m => digitsSelecter(m)))
                    .Concat(new[] { 1 })
                    .Max();
                // x と y に出現する連続する数字を、'0'を先頭にパディングすることによって桁数がmaxDigitsLength桁になるように置換する。
                Func<Match, string> replacer = s => s.Groups["digits"].Value.TrimStart('0').PadLeft(maxDigitsLength, '0');
                x = _digitsPattern.Replace(x, m => replacer(m));
                y = _digitsPattern.Replace(y, m => replacer(m));
            }
            if (_option.HasFlag(FilePathNameComparerrOption.ConsiderPathNameDelimiter))
            {
                // x と y をデリミタにより分割して、要素ごとに比較する。
                return
                    x.Split('/', '\\')
                    .SequenceCompare(
                        y.Split('/', '\\'),
                        new CustomizableComparer<string>(
                            (s1, s2) => InternalCompareForSimpleString(s1, s2)));
            }
            else
                return InternalCompareForSimpleString(x, y);
        }

        private static FilePathNameComparerrOption GetOptionFromStringComparison(StringComparison comparisonMethod)
        {
            if (comparisonMethod.IsAnyOf(
                StringComparison.CurrentCultureIgnoreCase,
                StringComparison.InvariantCultureIgnoreCase,
                StringComparison.OrdinalIgnoreCase))
            {
                return FilePathNameComparerrOption.IgnoreCase;
            }
            else
            {
                return FilePathNameComparerrOption.None;
            }
        }

        private static CultureInfo GetCultureFromStringComparison(StringComparison comparisonMethod)
        {
            if (comparisonMethod.IsAnyOf(StringComparison.CurrentCulture, StringComparison.CurrentCultureIgnoreCase))
                return CultureInfo.CurrentCulture;
            else if (comparisonMethod.IsAnyOf(StringComparison.InvariantCulture, StringComparison.InvariantCultureIgnoreCase))
                return CultureInfo.InvariantCulture;
            else
                return null;
        }

        private int InternalCompareForSimpleString(string x, string y)
        {
#if DEBUG
            if (x == null)
                throw new Exception();
            if (y == null)
                throw new Exception();
#endif
            var ignoreCase = _option.HasFlag(FilePathNameComparerrOption.IgnoreCase);
            if (_culture == null)
                return string.Compare(x, y, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
            else
                return string.Compare(x, y, ignoreCase, _culture);
        }
    }
}