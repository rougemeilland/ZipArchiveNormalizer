//#define TRACE_PARSER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Utility.Text
{
    class ReplaceImageTagEnumerable
        : IEnumerable<ShiftJisChar>
    {
        private enum CacheType
        {
            Invalid,
            Prefix,
            ImagePath,
            Suffix,
        }

        private interface IParserSource
        {
            ShiftJisChar PeekChar(Int32 index, bool causeExceptionOnUnexpectedEndOfSequence = true);
            ShiftJisChar ProcessChar(Func<ShiftJisChar, CacheType> matchHandler);
        }

        private class AozoraBunkoParser
        {
            private readonly IParserSource _source;

            public AozoraBunkoParser(IParserSource source)
            {
                _source = source;
            }

            public void Parse()
            {
                // The first character is not checked because it has been found to match.
                _source.ProcessChar(c => CacheType.Prefix); // process zenkaku '＃'

                // process image name (== alt attribute)
                while (true)
                {
                    if (!IsImageNameChar(_source.PeekChar(0)))
                        break;

                    // process image name char
                    _source.ProcessChar(c => CacheType.Prefix);
                }

                if (_source.PeekChar(0).Equals(0x8169)) // in case zenkaku '（'
                {
                    _source.ProcessChar(c => CacheType.Prefix); // process zenkaku '（'

                    // process image path
                    while (true)
                    {
                        if (!IsImagPathChar(_source.PeekChar(0)))
                            break;

                        // process image path char
                        _source.ProcessChar(c => CacheType.ImagePath);
                    }
                    if (_source.PeekChar(0).Equals(0x8141)) // in case zenkaku '、'
                    {
                        _source.ProcessChar(c => CacheType.Suffix); // process zenkaku '、'

                        while (true)
                        {
                            if (!IsImagSizeChar(_source.PeekChar(0)))
                                break;

                            // process image path char
                            _source.ProcessChar(c => CacheType.Suffix);
                        }
                    }
                    _source.ProcessChar(c => c.Equals(0x816a) ? CacheType.Suffix : CacheType.Invalid); // process zenkaku '）'
                    _source.ProcessChar(c => c.Equals(0x93fc) ? CacheType.Suffix : CacheType.Invalid); // process zenkaku '入'
                    _source.ProcessChar(c => c.Equals(0x82e9) ? CacheType.Suffix : CacheType.Invalid); // process zenkaku 'る'
                }
                _source.ProcessChar(c => c.Equals(0x816e) ? CacheType.Suffix : CacheType.Invalid); // process zenkaku '］'
            }

            private static bool IsImageNameChar(ShiftJisChar c) => !c.IsBetween(0x00, 0x1f) && c.IsNoneOf(0x8169, 0x816a, 0x816d, 0x816e); // control code, zenkaku '（', zenkaku '）', zenkaku '［', zenkaku '］'
            private static bool IsImagPathChar(ShiftJisChar c) => !c.IsBetween(0x00, 0x1f) && c.IsNoneOf(0x8169, 0x816a, 0x816d, 0x816e, 0x8141); // control code, zenkaku '（', zenkaku '）', zenkaku '［', zenkaku '］', zenkaku '、'
            private static bool IsImagSizeChar(ShiftJisChar c) => !c.IsBetween(0x00, 0x1f) && c.IsNoneOf(0x8169, 0x816a, 0x816d, 0x816e, 0x8141); // control code, zenkaku '（', zenkaku '）', zenkaku '［', zenkaku '］', zenkaku '、'
        }

        private class XhtmlParser
        {
            private readonly IParserSource _source;

            private bool _proccessedSrcAttribute;

            public XhtmlParser(IParserSource source)
            {
                _source = source;
                _proccessedSrcAttribute = false;
            }

            public void Parse()
            {
                _proccessedSrcAttribute = false;

                // Do not check the first 3 characters because they have been found to match.
                _source.ProcessChar(c => CacheType.Prefix); // process 'I' or 'i'
                _source.ProcessChar(c => CacheType.Prefix); // process 'M' or 'm'
                _source.ProcessChar(c => CacheType.Prefix); // process 'G' or 'g'
                _source.ProcessChar(c =>
                    !IsWhiteSpaceChar(c)
                    ? CacheType.Invalid
                    : _proccessedSrcAttribute
                    ? CacheType.Suffix
                    : CacheType.Prefix);

                // process attributes
                while (true)
                {
                    if (IsWhiteSpaceChar(_source.PeekChar(0)))
                    {
                        // skip white space
                        while (IsWhiteSpaceChar(_source.PeekChar(0)))
                            _source.ProcessChar(c => _proccessedSrcAttribute ? CacheType.Suffix : CacheType.Prefix);
                    }
                    else if (
                        _source.PeekChar(0).Equals(0x2f) || // '/'
                        _source.PeekChar(0).Equals(0x3e))   // '>'
                    {
                        break;
                    }
                    else if (IsAttributeNameChar(_source.PeekChar(0)))
                        ProcessAttribute();
                    else
                        throw new BadAozoraBunkoFormatException();
                }
                if (_source.PeekChar(0).Equals(0x2f) && // '/'
                    _source.PeekChar(1).Equals(0x3e))   // '>'
                {
                    _source.ProcessChar(c => CacheType.Suffix); // process '/'
                    _source.ProcessChar(c => CacheType.Suffix); // process '>'
                    return;
                }
                if (_source.PeekChar(0).Equals(0x3e))   // '>'
                {
                    _source.ProcessChar(c => CacheType.Suffix); // process '>'
                    return;
                }
            }

            private void ProcessAttribute()
            {
                var attributeNameChars = new RandomAccessQueue<ShiftJisChar>();

                while (true)
                {
                    if (!IsAttributeNameChar(_source.PeekChar(0)))
                        break;
                    attributeNameChars.Enqueue(_source.PeekChar(0));
                    _source.ProcessChar(c => _proccessedSrcAttribute ? CacheType.Suffix : CacheType.Prefix);
                }

                // skip white space
                while (IsWhiteSpaceChar(_source.PeekChar(0)))
                    _source.ProcessChar(c => _proccessedSrcAttribute ? CacheType.Suffix : CacheType.Prefix);

                if (!_source.PeekChar(0).Equals(0x3d)) // in case not '='
                {
                    // in case not exists attribute value
                    return;
                }
                _source.ProcessChar(c => _proccessedSrcAttribute ? CacheType.Suffix : CacheType.Prefix); // process '='

                // skip white space
                while (IsWhiteSpaceChar(_source.PeekChar(0)))
                    _source.ProcessChar(c => _proccessedSrcAttribute ? CacheType.Suffix : CacheType.Prefix);

                var isSrcTag =
                    attributeNameChars.Count == 3 &&
                    attributeNameChars[0].IsAnyOf(0x53, 0x73) &&    // 'S' or 's'
                    attributeNameChars[1].IsAnyOf(0x52, 0x72) &&    // 'R' or 'r'
                    attributeNameChars[2].IsAnyOf(0x43, 0x63);      // 'C' or 'c'

                if (_source.PeekChar(0).Equals(0x22)) // in case double quote
                {
                    // process double quote
                    _source.ProcessChar(c => isSrcTag ? CacheType.ImagePath : _proccessedSrcAttribute ? CacheType.Suffix : CacheType.Prefix);

                    while (true)
                    {
                        if (!IsDoubleQuotedAttributeValueChar(_source.PeekChar(0)))
                        {
                            if (isSrcTag)
                                _proccessedSrcAttribute = true;
                            break;
                        }
                        _source.ProcessChar(c => isSrcTag ? CacheType.ImagePath : _proccessedSrcAttribute ? CacheType.Suffix : CacheType.Prefix);
                    }

                    // process double quote
                    _source.ProcessChar(c => !c.Equals(0x22) ? CacheType.Invalid : isSrcTag ? CacheType.ImagePath : _proccessedSrcAttribute ? CacheType.Suffix : CacheType.Prefix);
                }
                else if (_source.PeekChar(0).Equals(0x27)) // in case single quote
                {
                    // process single quote
                    _source.ProcessChar(c => isSrcTag ? CacheType.ImagePath : _proccessedSrcAttribute ? CacheType.Suffix : CacheType.Prefix);

                    while (true)
                    {
                        if (!IsSingleQuotedAttributeValueChar(_source.PeekChar(0)))
                        {
                            if (isSrcTag)
                                _proccessedSrcAttribute = true;
                            break;
                        }
                        _source.ProcessChar(c => isSrcTag ? CacheType.ImagePath : _proccessedSrcAttribute ? CacheType.Suffix : CacheType.Prefix);
                    }

                    // process single quote
                    _source.ProcessChar(c => !c.Equals(0x27) ? CacheType.Invalid : isSrcTag ? CacheType.ImagePath : _proccessedSrcAttribute ? CacheType.Suffix : CacheType.Prefix);
                }
                else if (IsAttributeValueChar(_source.PeekChar(0))) // in case unenclosed attribute value char
                {
                    while (true)
                    {
                        if (!IsAttributeValueChar(_source.PeekChar(0)) ||                           // in case not attribute char
                            _source.PeekChar(0).Equals(0x2f) && _source.PeekChar(1).Equals(0x3e))   // in case "/>"
                        {
                            if (isSrcTag)
                                _proccessedSrcAttribute = true;
                            break;
                        }
                        _source.ProcessChar(c => isSrcTag ? CacheType.ImagePath : _proccessedSrcAttribute ? CacheType.Suffix : CacheType.Prefix);
                    }
                }
                else
                    throw new BadAozoraBunkoFormatException();
            }

            private static bool IsWhiteSpaceChar(ShiftJisChar c) => c.IsAnyOf(0x09, 0x0a, 0x0c, 0x0d, 0x20);
            private static bool IsAttributeNameChar(ShiftJisChar c) => !c.IsBetween(0x00, 0x20) && c.IsNoneOf(0x22, 0x27, 0x3e, 0x2f, 0x3d); // control code, ' ', '\"', '\'', '>', '/', '='
            private static bool IsAttributeValueChar(ShiftJisChar c) => !c.IsBetween(0x00, 0x20) && c.IsNoneOf(0x22, 0x27, 0x3c, 0x3e, 0x3d); // control code, ' ', '\"', '\'', '<', '>', '='
            private static bool IsSingleQuotedAttributeValueChar(ShiftJisChar c) => !c.IsBetween(0x00, 0x1f) && c.IsNoneOf(0x27, 0x3c, 0x3e); // control code, '\'', '<', '>'
            private static bool IsDoubleQuotedAttributeValueChar(ShiftJisChar c) => !c.IsBetween(0x00, 0x1f) && c.IsNoneOf(0x22, 0x3c, 0x3e); // control code, '\"', '<', '>'
        }

        private class Enumerator
            : IEnumerator<ShiftJisChar>, IParserSource
        {
            private static readonly Encoding _shiftJisEncoding;

            private readonly IEnumerable<ShiftJisChar> _source;
            private readonly Func<string, string> _replacer;
            private readonly AozoraBunkoParser _aozoraBunkoParser;
            private readonly XhtmlParser _xhtmlParser;
            private readonly Queue<ShiftJisChar> _prefix;
            private readonly Queue<ShiftJisChar> _imagePath;
            private readonly Queue<ShiftJisChar> _suffix;
            private readonly RandomAccessQueue<ShiftJisChar> _sourceCache;

            private bool _isDisposed;
            private IEnumerator<ShiftJisChar> _sourceEnumerator;
            private ShiftJisChar? _currentValue;
            private bool _isEndOfSequence;

            static Enumerator()
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                _shiftJisEncoding = Encoding.GetEncoding("shift_jis");
            }

            public Enumerator(IEnumerable<ShiftJisChar> source, Func<string, string> replacer)
            {
                _isDisposed = false;
                _source = source;
                _replacer = replacer;
                _aozoraBunkoParser = new AozoraBunkoParser(this);
                _xhtmlParser = new XhtmlParser(this);
                _sourceEnumerator = _source.GetEnumerator();
                _prefix = new Queue<ShiftJisChar>();
                _imagePath = new Queue<ShiftJisChar>();
                _suffix = new Queue<ShiftJisChar>();
                _sourceCache = new RandomAccessQueue<ShiftJisChar>();
                _currentValue = null;
                _isEndOfSequence = false;
            }

            public ShiftJisChar Current
            {
                get
                {
                    if (_isDisposed)
                        throw new ObjectDisposedException(GetType().FullName);
                    if (_isEndOfSequence)
                        throw new InvalidOperationException();

                    return _currentValue ?? throw new InvalidOperationException();
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                _currentValue = null;
                if (_isEndOfSequence)
                    return false;
                if (_prefix.Any())
                {
                    _currentValue = _prefix.Dequeue();
                    return true;
                }
                if (_imagePath.Any())
                {
                    _currentValue = _imagePath.Dequeue();
                    return true;
                }
                if (_suffix.Any())
                {
                    _currentValue = _suffix.Dequeue();
                    return true;
                }

                // Load the first 4 characters into the cache to determine the type of tag.
                FillCache(4);

#if DEBUG && TRACE_PARSER
                System.Diagnostics.Debug.Write(string.Format("first: {{{0}}}, ", _sourceCache.Length <= 0 ? "null" : GetFriendlyString(_sourceCache[0])));
#endif
                if (_sourceCache.Count <= 0)
                {
                    _isEndOfSequence = true;
                    return false;
                }
                if (_sourceCache.Length >= 2 &&
                    _sourceCache[0].Equals(0x816d) &&   // zenkaku '［'
                    _sourceCache[1].Equals(0x8194))     // zenkaku '＃'
                {
                    try
                    {
                        _currentValue = _sourceCache.Dequeue();
                        _aozoraBunkoParser.Parse();
                        if (_imagePath.Any())
                        {
                            var imagePath = _shiftJisEncoding.GetString(_imagePath.EncodeAsShiftJisChar().ToArray());
                            imagePath = ReplaceImagePath(imagePath);
                            _imagePath.Clear();
                            foreach (var c in _shiftJisEncoding.GetBytes(imagePath).DecodeAsShiftJisChar())
                                _imagePath.Enqueue(c);
                        }
                    }
                    catch (UnexpectedEndOfSequenceException)
                    {
                        Fallback();
                    }
                    catch (BadAozoraBunkoFormatException)
                    {
                        Fallback();
                    }
                }
                else if (
                    _sourceCache.Length >= 4 &&
                    _sourceCache[0].Equals(0x3c) &&         // hankaku '<'
                    _sourceCache[1].IsAnyOf(0x49, 0x69) &&  // hankaku 'I' or 'i'
                    _sourceCache[2].IsAnyOf(0x4d, 0x6d) &&  // hankaku 'M' or 'm'
                    _sourceCache[3].IsAnyOf(0x47, 0x67))    // hankaku 'G' or 'g'
                {
                    try
                    {
                        _currentValue = _sourceCache.Dequeue();
                        _xhtmlParser.Parse();
                        if (_imagePath.Any())
                        {
                            var imagePath = _shiftJisEncoding.GetString(_imagePath.EncodeAsShiftJisChar().ToArray());
                            var enclosureChar =
                                imagePath.StartsWith("\"", StringComparison.Ordinal) && imagePath.EndsWith("\"", StringComparison.Ordinal)
                                ? "\""
                                : imagePath.StartsWith("\'", StringComparison.Ordinal) && imagePath.EndsWith("\'", StringComparison.Ordinal)
                                ? "\'"
                                : "";
                            imagePath = imagePath.Substring(enclosureChar.Length, imagePath.Length - enclosureChar.Length * 2);
                            imagePath = ReplaceImagePath(WebUtility.HtmlDecode(imagePath));
                            if (enclosureChar == "" && imagePath.Contains(' '))
                            {
                                var containsSingleQuote = imagePath.Contains('\'');
                                var containsDoubleQuote = imagePath.Contains('"');
                                if (containsSingleQuote)
                                {
                                    if (containsDoubleQuote)
                                    {
                                        // In case where the modified attribute value contains both single and double quotes
                                        throw new BadAozoraBunkoFormatException();
                                    }
                                    else
                                    {
                                        // In cases where the modified attribute value contains only single quotes
                                        enclosureChar = "\"";
                                    }
                                }
                                else
                                {
                                    if (containsDoubleQuote)
                                    {
                                        // In cases where the modified attribute value contains only double quotes
                                        enclosureChar = "\'";
                                    }
                                    else
                                    {
                                        // In cases where the modified attribute value contains neither double quotes nor double quotes
                                        enclosureChar = "\"";
                                    }
                                }
                            }
                            imagePath = enclosureChar + WebUtility.HtmlEncode(imagePath) + enclosureChar;
                            _imagePath.Clear();
                            foreach (var c in _shiftJisEncoding.GetBytes(imagePath).DecodeAsShiftJisChar())
                                _imagePath.Enqueue(c);
                        }
                    }
                    catch (UnexpectedEndOfSequenceException)
                    {
                        Fallback();
                    }
                    catch (BadAozoraBunkoFormatException)
                    {
                        Fallback();
                    }
                }
                else
                    _currentValue = _sourceCache.Dequeue();
                return true;
            }

            public void Reset()
            {
                _sourceEnumerator?.Dispose();
                _sourceEnumerator = _source.GetEnumerator();
                _prefix.Clear();
                _imagePath.Clear();
                _suffix.Clear();
                _sourceCache.Clear();
                _currentValue = null;
                _isEndOfSequence = false;
            }

            public void FillCache(Int32 index)
            {
                while (_sourceCache.Count <= index)
                {
                    if (!_sourceEnumerator.MoveNext())
                        return;
                    _sourceCache.Enqueue(_sourceEnumerator.Current);
                }
            }

            public ShiftJisChar PeekChar(Int32 index, bool causeExceptionOnUnexpectedEndOfSequence = true)
            {
                while (_sourceCache.Count <= index)
                {
                    if (!_sourceEnumerator.MoveNext())
                    {
                        if (causeExceptionOnUnexpectedEndOfSequence)
                            throw new UnexpectedEndOfSequenceException();
                        break;
                    }
                    _sourceCache.Enqueue(_sourceEnumerator.Current);
                }
                return _sourceCache[index];
            }

            public ShiftJisChar ProcessChar(Func<ShiftJisChar, CacheType> matchHandler)
            {
                var c =
                    _sourceCache.Any()
                    ? _sourceCache.Dequeue()
                    : _sourceEnumerator.MoveNext()
                    ? _sourceEnumerator.Current
                    : throw new UnexpectedEndOfSequenceException();
#if DEBUG && TRACE_PARSER
                System.Diagnostics.Debug.Write(string.Format("{{{0}}}, ",GetFriendlyString(c)));
#endif
                switch (matchHandler(c))
                {
                    case CacheType.Prefix:
                        _prefix.Enqueue(c);
                        break;
                    case CacheType.ImagePath:
                        _imagePath.Enqueue(c);
                        break;
                    case CacheType.Suffix:
                        _suffix.Enqueue(c);
                        break;
                    case CacheType.Invalid:
                    default:
                        _suffix.Enqueue(c);
                        throw new BadAozoraBunkoFormatException();
                }
                return c;
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_isDisposed)
                {
                    if (disposing)
                        _sourceEnumerator.Dispose();
                    _isDisposed = true;
                }
            }

            private string ReplaceImagePath(string imagePath)
            {
                try
                {
                    imagePath = _replacer(imagePath);
                }
                catch (Exception)
                {
                }

                return imagePath;
            }

            private void Fallback()
            {
#if DEBUG && TRACE_PARSER
                System.Diagnostics.Debug.WriteLine("");
                System.Diagnostics.Debug.WriteLine("fallback");
#endif
                var copyOfsouceQueue = _sourceCache.ToList();
                _sourceCache.Clear();
                foreach (var c in _prefix)
                {
#if DEBUG && TRACE_PARSER
                    System.Diagnostics.Debug.WriteLine(string.Format("redo {{{0}}} (from prefix)", GetFriendlyString(c)));
#endif
                    _sourceCache.Enqueue(c);
                }
                foreach (var c in _imagePath)
                {
#if DEBUG && TRACE_PARSER
                    System.Diagnostics.Debug.WriteLine(string.Format("redo {{{0}}} (from image path)", GetFriendlyString(c)));
#endif
                    _sourceCache.Enqueue(c);
                }
                foreach (var c in _suffix)
                {
#if DEBUG && TRACE_PARSER
                    System.Diagnostics.Debug.WriteLine(string.Format("redo {{{0}}} (from suffix)", GetFriendlyString(c)));
#endif
                    _sourceCache.Enqueue(c);
                }
                foreach (var c in copyOfsouceQueue)
                {
#if DEBUG && TRACE_PARSER
                    System.Diagnostics.Debug.WriteLine(string.Format("redo {{{0}}} (from remaining of source cache)", c));
#endif
                    _sourceCache.Enqueue(c);
                }
                _prefix.Clear();
                _imagePath.Clear();
                _suffix.Clear();
            }

#if DEBUG && TRACE_PARSER
            private string GetFriendlyString(ShiftJisChar c)
            {
                return
                    c.Equals(0x0d)
                    ? "\\r"
                    : c.Equals(0x0a)
                    ? "\\n"
                    : c.Equals(0x09)
                    ? "\\t"
                    : c.ToString();
            }
#endif
        }

        private readonly IEnumerable<ShiftJisChar> _source;
        private readonly Func<string, string> _replacer;

        public ReplaceImageTagEnumerable(IEnumerable<ShiftJisChar> source, Func<string, string> replacer)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (replacer is null)
                throw new ArgumentNullException(nameof(replacer));

            _source = source;
            _replacer = replacer;
        }

        public IEnumerator<ShiftJisChar> GetEnumerator() => new Enumerator(_source, _replacer);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
