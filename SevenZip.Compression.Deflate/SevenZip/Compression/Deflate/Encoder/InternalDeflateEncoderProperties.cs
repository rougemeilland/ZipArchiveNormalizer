using System;
using Utility;

namespace SevenZip.Compression.Deflate.Encoder
{
    class InternalDeflateEncoderProperties
        : CoderProperties
    {
        private enum PropertyId
        {
            Level,
            NumFastBytes,
            MatchFinder,
            MatchFinderCycles,
            NumPasses,
            Algorithm,
        }

        private class PropertiesNormalizer
            : IPropertiesNormalizer
        {
            private readonly bool _deflate64Mode;

            public PropertiesNormalizer(bool deflate64Mode)
            {
                _deflate64Mode = deflate64Mode;
            }

            bool IPropertiesNormalizer.IsValidValue(INormalizedPropertiesSource properties, Enum propertyId, object value)
            {
                return
                    propertyId switch
                    {
                        PropertyId.Level => ((Int32)value).IsBetween(0, 9),
                        PropertyId.NumFastBytes => ((UInt32)value).IsBetween((UInt32)DeflateConstants.kMatchMinLen, _deflate64Mode ? DeflateConstants.kMatchMaxLen64 : DeflateConstants.kMatchMaxLen32),
                        PropertyId.MatchFinder => ((MatchFinderType)value).IsAnyOf(MatchFinderType.HC3ZIP, MatchFinderType.BT3ZIP),
                        _ => true,
                    };
            }

            void IPropertiesNormalizer.Normalize(INormalizedPropertiesSource properties)
            {
                if (!properties.IsSet(PropertyId.Level))
                    properties[PropertyId.Level] = 5;
                var level = (Int32)properties[PropertyId.Level];
                if (!properties.IsSet(PropertyId.Algorithm))
                    properties[PropertyId.Algorithm] = level >= 5;
                if (!properties.IsSet(PropertyId.NumFastBytes))
                    properties[PropertyId.NumFastBytes] =
                        level < 7
                            ? 32U
                            : level < 9
                                ? 64U
                                : 128U;
                var algorithm = (bool)properties[PropertyId.Algorithm];
                if (!properties.IsSet(PropertyId.MatchFinder))
                    properties[PropertyId.MatchFinder] = algorithm ? MatchFinderType.BT3ZIP : MatchFinderType.HC3ZIP;
                var numFastBytes = (UInt32)properties[PropertyId.NumFastBytes];
                if (!properties.IsSet(PropertyId.MatchFinderCycles))
                    properties[PropertyId.MatchFinderCycles] = 16 + (numFastBytes >> 1);
                if (!properties.IsSet(PropertyId.NumPasses))
                    properties[PropertyId.NumPasses] =
                        level < 7
                            ? 1U
                            : level < 9
                                ? 3U
                                : 10U;
            }
        }

        public InternalDeflateEncoderProperties(bool deflateMode)
        {
            Normalizer = new PropertiesNormalizer(deflateMode);
        }

        /// <summary>
        /// Specifies compression Level (0 &lt;= x &lt;= 9).
        /// </summary>
        public Int32 Level { get => (Int32)GetValue(PropertyId.Level); set => SetValue(PropertyId.Level, value); }

        /// <summary>
        /// <summary>
        /// Specifies number of fast bytes for LZ*.
        /// </summary>
        public UInt32 NumFastBytes { get => (UInt32)GetValue(PropertyId.NumFastBytes); set => SetValue(PropertyId.NumFastBytes, value); }

        /// <summary>
        /// Specifies match finder. (any of <see cref="MatchFinderType.HC3ZIP"/> / <see cref="MatchFinderType.BT3ZIP"/>)
        /// </summary>
        public MatchFinderType MatchFinder { get => (MatchFinderType)GetValue(PropertyId.MatchFinder); set => SetValue(PropertyId.MatchFinder, value); }

        /// <summary>
        /// Specifies the number of match finder cyckes.
        /// </summary>
        public UInt32 MatchFinderCycles { get => (UInt32)GetValue(PropertyId.MatchFinderCycles); set => SetValue(PropertyId.MatchFinderCycles, value); }

        /// <summary>
        /// Specifies number of passes.
        /// </summary>
        public UInt32 NumPasses { get => (UInt32)GetValue(PropertyId.NumPasses); set => SetValue(PropertyId.NumPasses, value); }

        /// <summary>
        /// Specifies false for fast mode, true for normal mode.
        /// </summary>
        public bool Algorithm { get => (bool)GetValue(PropertyId.Algorithm); set => SetValue(PropertyId.Algorithm, value); }

        protected override IPropertiesNormalizer Normalizer { get; }
    }
}
