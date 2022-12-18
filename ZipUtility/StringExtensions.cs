using System;
using System.Linq;
using System.Text;

namespace ZipUtility
{
    public static class StringExtensions
    {
        private static readonly Encoding _zipStandardEncoding;

        static StringExtensions()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _zipStandardEncoding =
                Encoding.GetEncoding(
                    "IBM437",
                    new EncoderReplacementFallback("@"),
                    new DecoderReplacementFallback("@"));
        }

        public static bool IsConvertableToMinimumCharacterSet(this string s)
        {
            return
                s is null ||
                Encoding.UTF8.GetBytes(s).SequenceEqual(_zipStandardEncoding.GetBytes(s));
        }
    }
}