using System;
using Utility;

namespace SevenZip.Compression.Lzma.Encoder
{
    public class LzmaEncoderProperties
        : CoderProperties
    {
        private enum PropertyId
        {
            Level,
            DictionarySize,
            PosStateBits,
            LitContextBits,
            LitPosBits,
            NumFastBytes,
            MatchFinder,
            MatchFinderCycles,
            // Algorithm, // not used as only BT2 and BT4 are supported
            // ReduceSize, // not used
            EndMarker,
        }

        private class PropertiesNormalizer
            : IPropertiesNormalizer
        {
            bool IPropertiesNormalizer.IsValidValue(INormalizedPropertiesSource properties, Enum propertyId, object value)
            {
                return
                    propertyId switch
                    {
                        PropertyId.Level => ((Int32)value).IsBetween(0, 9),
                        PropertyId.DictionarySize => ((UInt32)value).IsBetween(1U << 12, 1U << 27),
                        PropertyId.PosStateBits => ((Int32)value).IsBetween(0, 4),
                        PropertyId.LitContextBits => ((Int32)value).IsBetween(0, 8),
                        PropertyId.LitPosBits => ((Int32)value).IsBetween(0, 4),
                        PropertyId.NumFastBytes => ((UInt32)value).IsBetween(5U, 273U),
                        PropertyId.MatchFinder => ((MatchFinderType)value).IsNoneOf(/*MatchFinderType.HC4, MatchFinderType.HC5, */MatchFinderType.BT2, /*MatchFinderType.BT3, */MatchFinderType.BT4/*, MatchFinderType.BT5*/),
                        PropertyId.MatchFinderCycles => ((UInt32)value).IsBetween(1U, 1U << 30),
                        _ => true,
                    };
            }

            void IPropertiesNormalizer.Normalize(INormalizedPropertiesSource properties)
            {
                if (!properties.IsSet(PropertyId.Level))
                    properties[PropertyId.Level] = 5;
                var level = (Int32)properties[PropertyId.Level];
                if (!properties.IsSet(PropertyId.DictionarySize))
                    properties[PropertyId.DictionarySize] =
                        level <= 3
                            ? (1U << (level * 2 + 16))
                            : (level <= 6
                                ? (1U << (level + 19))
                                : level <= 7
                                    ? (1U << 25)
                                    : (1U << 26));
                var dictionarySize = (UInt32)properties[PropertyId.DictionarySize];
#if false // not used
                if (!properties.IsSet(PropertyId.ReduceSize))
                    properties[PropertyId.ReduceSize] = UInt64.MaxValue;
                var reduceSize = (UInt64)properties[PropertyId.ReduceSize];
                if (dictionarySize > reduceSize)
                {
                    dictionarySize = (UInt32)reduceSize.Maximum(1U << 12).Minimum(dictionarySize);
                    properties[PropertyId.DictionarySize] = dictionarySize;
                }
#endif
                if (!properties.IsSet(PropertyId.LitContextBits))
                    properties[PropertyId.LitContextBits] = 3;
                if (!properties.IsSet(PropertyId.LitPosBits))
                    properties[PropertyId.LitPosBits] = 0;
                if (!properties.IsSet(PropertyId.PosStateBits))
                    properties[PropertyId.PosStateBits] = 2;
#if false // not used as only BT2 and BT4 are supported
                if (!properties.IsSet(PropertyId.Algorithm))
                    properties[PropertyId.Algorithm] = level >= 5;
                var algorithm = (bool)properties[PropertyId.Algorithm];
#endif
                if (!properties.IsSet(PropertyId.NumFastBytes))
                    properties[PropertyId.NumFastBytes] = level < 7 ? 32U : 64U;
                if (!properties.IsSet(PropertyId.MatchFinder))
                {
#if true // only BT2 and BT4 are supported
                    properties[PropertyId.MatchFinder] = level < 5 ? MatchFinderType.BT2 : MatchFinderType.BT4;
#else
                    properties[PropertyId.MatchFinder] = algorithm ? MatchFinderType.BT4 : MatchFinderType.BT5;
#endif
                }
                var matchFinder = (MatchFinderType)properties[PropertyId.MatchFinder];
                if (!properties.IsSet(PropertyId.MatchFinderCycles))
                    properties[PropertyId.MatchFinderCycles] = (16 + (GetNumHashBytes(matchFinder) >> 1)) >> (GetBtMode(matchFinder) ? 0 : 1);
                if (!properties.IsSet(PropertyId.EndMarker))
                    properties[PropertyId.EndMarker] = false;
            }
        }

        public LzmaEncoderProperties()
        {
            Normalizer = new PropertiesNormalizer();
        }

        /// <summary>
        /// Specifies compression Level (0 &lt;= <see cref="Level"/> &lt;= 9).
        /// </summary>
        public Int32 Level { get => (Int32)GetValue(PropertyId.Level); set => SetValue(PropertyId.Level, value); }

        /// <summary>
        /// Specifies size of dictionary (0x1000 &lt;= <see cref="DictionarySize"/> &lt;= 0x8000000, default = 0x1000000).
        /// </summary>
        public Int32 DictionarySize { get => (Int32)GetValue(PropertyId.DictionarySize); set => SetValue(PropertyId.DictionarySize, value); }

        /// <summary>
        /// Specifies number of postion state bits (0 &lt;= <see cref="PosStateBits"/> &lt;= 4, default = 2).
        /// </summary>
        public Int32 PosStateBits { get => (Int32)GetValue(PropertyId.PosStateBits); set => SetValue(PropertyId.PosStateBits, value); }

        /// <summary>
        /// Specifies number of literal context bits (0 &lt;= <see cref="LitContextBits"/> &lt;= 8, default = 3).
        /// </summary>
        public Int32 LitContextBits { get => (Int32)GetValue(PropertyId.LitContextBits); set => SetValue(PropertyId.LitContextBits, value); }

        /// <summary>
        /// Specifies number of literal position bits (0 &lt;= <see cref="LitPosBits"/> &lt;= 4, default = 0).
        /// </summary>
        public Int32 LitPosBits { get => (Int32)GetValue(PropertyId.LitPosBits); set => SetValue(PropertyId.LitPosBits, value); }

        /// <summary>
        /// Specifies number of fast bytes (5 &lt;= <see cref="NumFastBytes"/> &lt;= 273, default = 32).
        /// </summary>
        public Int32 NumFastBytes { get => (Int32)GetValue(PropertyId.NumFastBytes); set => SetValue(PropertyId.NumFastBytes, value); }

#if false // not used as only BT2 and BT4 are supported
        /// <summary>
        /// false: hashChain Mode, true: binTree mode, default = true
        /// </summary>
        public bool BtMode => GetBtMode(MatchFinder);
#endif

#if false // not used as only BT2 and BT4 are supported
        /// <summary>
        /// Nnumber of hash bytes
        /// </summary>
        public UInt32 NumHashBytes => GetNumHashBytes(MatchFinder);
#endif

        /// <summary>
        /// Specifies match finder. (any of <see cref="MatchFinderType.HC4"/> / <see cref="MatchFinderType.HC5"/> / <see cref="MatchFinderType.BT2"/> / <see cref="MatchFinderType.BT3"/> / <see cref="MatchFinderType.BT4"/> / <see cref="MatchFinderType.BT5"/>, default = <see cref="MatchFinderType.HC5"/> / <see cref="MatchFinderType.BT4"/>)
        /// </summary>
        public MatchFinderType MatchFinder
        {
            get => (MatchFinderType)GetValue(PropertyId.MatchFinder);
            set
            {
                switch (value)
                {
                    case MatchFinderType.BT2:
                    // case MatchFinderType.BT3: // not supported
                    case MatchFinderType.BT4:
                        // case MatchFinderType.BT5: // not supported
                        // case MatchFinderType.HC4: // not supported
                        // case MatchFinderType.HC5: // not supported
                        SetValue(PropertyId.MatchFinder, value);
                        break;
                    default:
                        throw new ArgumentException("Not supported match finder type", nameof(value));
                }
            }
        }

        /// <summary>
        /// Specifies the number of match finder cycles (1 &lt;= <see cref="MatchFinderCycles"/> &lt; 0x40000000, default = 32).
        /// </summary>
        public UInt32 MatchFinderCycles { get => (UInt32)GetValue(PropertyId.MatchFinderCycles); set => SetValue(PropertyId.MatchFinderCycles, value); }

#if false // not used as only BT2 and BT4 are supported
        /// <summary>
        /// Specifies false for fast mode, true for normal mode. default = true.
        /// </summary>
        public bool Algorithm { get => (bool)GetValue(PropertyId.Algorithm); set => SetValue(PropertyId.Algorithm, value); }
#endif

#if false // not used
        /// <summary>
        /// Estimated size of data that will be compressed (default: <see cref="UInt64.MaxValue"/>).
        /// </summary>
        /// <remarks>
        /// Encoder uses <see cref="ReduceSize"/> to reduce dictionary size.
        /// If you know the length of the data to be compressed in advance, set that length in <see cref="ReduceSize"/>.
        /// </remarks>
        public UInt64 ReduceSize { get => (UInt64)GetValue(PropertyId.ReduceSize); set => SetValue(PropertyId.ReduceSize, value); }
#endif

        /// <summary>
        /// Specifies mode with end marker (false: do not write EOPM, true: write EOPM, default = false).
        /// </summary>
        public bool EndMarker { get => (bool)GetValue(PropertyId.EndMarker); set => SetValue(PropertyId.EndMarker, value); }

        protected override IPropertiesNormalizer Normalizer { get; }

        private static bool GetBtMode(MatchFinderType matchFinder)
        {
            return matchFinder switch
            {
                MatchFinderType.BT2 => true,
                // MatchFinderType.BT3 => true, // not supported
                MatchFinderType.BT4 => true,
                // MatchFinderType.BT5 => true, // not supported
                // MatchFinderType.HC4 => false, // not supported
                // MatchFinderType.HC5 => false, // not supported
                _ => throw new InvalidOperationException(),
            };
        }

        private static UInt32 GetNumHashBytes(MatchFinderType matchFinderType)
        {
            return matchFinderType switch
            {
                MatchFinderType.BT2 => 2,
                // MatchFinderType.BT3 => 3, // not supported
                MatchFinderType.BT4 => 4,
                // MatchFinderType.HC4 => 4, // not supported
                // MatchFinderType.BT5 => 5, // not supported
                // MatchFinderType.HC5 => 5, // not supported
                _ => throw new InvalidOperationException(),
            };
        }
    }
}
