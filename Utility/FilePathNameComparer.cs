using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Utility
{
    public class FilePathNameComparer
        : IComparer<string>
    {
        private enum FleNameCharacterType
        {
            EOS = 0, // 文字列の終端
            Delimiters = 1, // パスの区切り文字
            ASCIILetters1 = 2, // 0x01から0x2fまでのASCII文字 (パスの区切り文字を除く)
            Digits = 3, // 数字
            ASCIILetters2 = 4, // 0x3aから0x7fまでのASCII文字 (パスの区切り文字を除く)
            MultiByteLetters = 5, // ASCII以外の文字
        }

        private class FileNameSegment
        {
            private string _text;

            public FileNameSegment(FleNameCharacterType type, string text)
            {
                Type = type;
                _text = text;
            }

            public FleNameCharacterType Type { get; }
            public string Text => Type != FleNameCharacterType.EOS ? _text : throw new InvalidOperationException();

            public override string ToString()
            {
                return
                    string.Format(
                        Type != FleNameCharacterType.EOS ? "{{Type={0}, Text=\"{1}\"}}" : "{{Type={0}}}",
                        Type,
                        _text);
            }
        }


        private static Regex _delimiterPattern;
        private static Regex _multiByteLetterPattern;
        private Regex _digitsPattern;
        private Regex _asciiLetter1Pattern;
        private Regex _asciiLetter2Pattern;
        private FilePathNameComparerrOption _option;
        private Func<string, string, int?> _customComparer;

        static FilePathNameComparer()
        {
            _delimiterPattern = new Regex(@"\G(?<lettres>[/\\]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _multiByteLetterPattern = new Regex(@"\G(?<lettres>[^\x01-\x7f]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        public FilePathNameComparer()
            : this(FilePathNameComparerrOption.None, null)
        {
        }

        public FilePathNameComparer(FilePathNameComparerrOption option)
            : this(option, null)
        {
        }

        public FilePathNameComparer(Func<string, string, int?> customComparer)
            : this(FilePathNameComparerrOption.None, customComparer)
        {
        }

        public FilePathNameComparer(FilePathNameComparerrOption option, Func<string, string, int?> customComparer)
        {
            _option = option;
            _customComparer = customComparer;
            if ((_option & FilePathNameComparerrOption.ConsiderSequenceOfDigitsAsNumber) != FilePathNameComparerrOption.None)
            {
                _asciiLetter1Pattern = new Regex(@"\G(?<lettres>[\x01-\x2e]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                _digitsPattern = new Regex(@"\G(?<lettres>[0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                _asciiLetter2Pattern = new Regex(@"\G(?<lettres>[\x3a-\x5b\x5d-\x7f]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            else
            {
                _asciiLetter1Pattern = new Regex(@"\G(?<lettres>[\x01-\x2e\x30-\x5b\x5d-\x7f]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                _digitsPattern = null;
                _asciiLetter2Pattern = null;
            }
        }

        private int CompareContentFiles(string x, string y)
        {
            if (x == "mimetype")
                return y == "mimetype" ? 0 : -1;
            else if (x == "META-INF")
            {
                if (y == "mimetype")
                    return 1;
                else if (y == "META-INF")
                    return 0;
                else
                    return -1;
            }
            else
            {
                return
                    y == "mimetype" || y == "META-INF"
                    ? 1
                    : Compare(x, y);
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
            var x_seguments = new Stack<FileNameSegment>(ParseFilePathName(x).Reverse());
            var y_seguments = new Stack<FileNameSegment>(ParseFilePathName(y).Reverse());
            if (_customComparer != null)
            {
                var c = _customComparer(x, y);
                if (c.HasValue)
                    return c.Value;
            }
            return Compare(x_seguments, y_seguments, x, y);
        }

        private int Compare(Stack<FileNameSegment> x_array, Stack<FileNameSegment> y_array, string x, string y)
        {
            while (x_array.Count > 0 && y_array.Count > 0)
            {
                var x_element = x_array.Pop();
                var y_element = y_array.Pop();
                int c;

                if ((c = x_element.Type.CompareTo(y_element.Type)) != 0)
                    return c;
                switch (x_element.Type)
                {
                    case FleNameCharacterType.EOS:
                        return 0;
                    case FleNameCharacterType.Delimiters:
                        if ((c = x_element.Text.CompareTo(y_element.Text)) != 0)
                            return c;
                        break;
                    case FleNameCharacterType.Digits:
                        if ((c = BigInteger.Parse(x_element.Text, NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat).CompareTo(BigInteger.Parse(y_element.Text, NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat))) != 0)
                            return c;
                        break;
                    default:
                        if (string.Equals(x_element.Text, y_element.Text, StringComparison.InvariantCultureIgnoreCase))
                            break;
                        else if (x_element.Text.Length > y_element.Text.Length && x_element.Text.StartsWith(y_element.Text, StringComparison.InvariantCultureIgnoreCase))
                        {
                            x_array.Push(new FileNameSegment(x_element.Type, x_element.Text.Substring(y_element.Text.Length)));
                            break;
                        }
                        else if (y_element.Text.Length > x_element.Text.Length && y_element.Text.StartsWith(x_element.Text, StringComparison.InvariantCultureIgnoreCase))
                        {
                            y_array.Push(new FileNameSegment(y_element.Type, y_element.Text.Substring(x_element.Text.Length)));
                            break;
                        }
                        else
                        {
                            if ((c = x_element.Text.CompareTo(y_element.Text)) != 0)
                                return c;
                        }
                        break;
                }
            }
            return x_array.Count.CompareTo(y_array.Count);
        }

        private IEnumerable<FileNameSegment> ParseFilePathName(string filePathName)
        {
            var segments = new List<FileNameSegment>();
            var index = 0;
            while (index < filePathName.Length)
            {
                Match m;
                if (_digitsPattern != null &&
                    (m = _digitsPattern.Match(filePathName, index)).Success)
                {
                    var text = m.Groups["lettres"].Value;
                    segments.Add(new FileNameSegment(FleNameCharacterType.Digits, text));
                    index += text.Length;
                }
                else if ((m = _delimiterPattern.Match(filePathName, index)).Success)
                {
                    var text = m.Groups["lettres"].Value;
                    segments.Add(new FileNameSegment(FleNameCharacterType.Delimiters, new string('/', text.Length)));
                    index += text.Length;
                }
                else if ((m = _multiByteLetterPattern.Match(filePathName, index)).Success)
                {
                    var text = m.Groups["lettres"].Value;
                    segments.Add(new FileNameSegment(FleNameCharacterType.MultiByteLetters, text));
                    index += text.Length;
                }
                else if ((m = _asciiLetter1Pattern.Match(filePathName, index)).Success)
                {
                    var text = m.Groups["lettres"].Value;
                    segments.Add(new FileNameSegment(FleNameCharacterType.ASCIILetters1, text));
                    index += text.Length;
                }
                else if (_asciiLetter2Pattern != null &&
                         (m = _asciiLetter2Pattern.Match(filePathName, index)).Success)
                {
                    var text = m.Groups["lettres"].Value;
                    segments.Add(new FileNameSegment(FleNameCharacterType.ASCIILetters2, text));
                    index += text.Length;
                }
                else
                {
                    throw new Exception();
                }
            }
            segments.Add(new FileNameSegment(FleNameCharacterType.EOS, null));
            return segments;
        }

        int IComparer<string>.Compare(string x, string y)
        {
            if (x == null)
                return y == null ? 0 : -1;
            else if (y == null)
                return 1;
            else if ((_option & FilePathNameComparerrOption.ContainsContentFile) != FilePathNameComparerrOption.None)
                return CompareContentFiles(x, y);
            else
                return Compare(x, y);
        }
    }
}
