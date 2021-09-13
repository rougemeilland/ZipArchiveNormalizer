using System.Linq;
using System.Text;

namespace ZipUtility
{
    public static class StringExtensions
    {
        private static Encoding _zipStandardEncoding;

        static StringExtensions()
        {
            _zipStandardEncoding =
                Encoding.GetEncoding(
                    "IBM437",
                    new EncoderReplacementFallback("@"),
                    new DecoderReplacementFallback("@"));
        }

        public static bool IsConvertableToMinimumCharacterSet(this string s)
        {
            return
                s == null ||
                Encoding.UTF8.GetBytes(s).SequenceEqual(_zipStandardEncoding.GetBytes(s));
        }
    }
}