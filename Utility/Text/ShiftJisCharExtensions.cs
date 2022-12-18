using System;
using System.Collections.Generic;
using System.Linq;

namespace Utility.Text
{
    public static class ShiftJisCharExtensions
    {
        public static IEnumerable<ShiftJisChar> DecodeAsShiftJisChar(this IEnumerable<byte> sequence)
        {
            if (sequence is null)
                throw new ArgumentNullException(nameof(sequence));

            return new ShiftJIsCharSequenceFromByteSequenceEnumerable(sequence);
        }

        public static IEnumerable<byte> EncodeAsShiftJisChar(this IEnumerable<ShiftJisChar> sequence)
        {
            if (sequence is null)
                throw new ArgumentNullException(nameof(sequence));
            return
                sequence
                .SelectMany(c => c.ToByteArray().GetSequence());
        }

        public static IEnumerable<ShiftJisChar> ReplaceAozoraBunkoImageTag(this IEnumerable<ShiftJisChar> sequence, Func<string, string> replacer)
        {
            if (sequence is null)
                throw new ArgumentNullException(nameof(sequence));
            if (replacer is null)
                throw new ArgumentNullException(nameof(replacer));

            return new ReplaceImageTagEnumerable(sequence, replacer);
        }
    }
}
