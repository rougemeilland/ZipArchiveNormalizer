using System;

namespace SevenZip.Compression.Deflate.Encoder
{
    public class Deflate64EncoderProperties
    {
        public Deflate64EncoderProperties()
        {
            InternalProperties = new InternalDeflateEncoderProperties(true);
        }

        /// <summary>
        /// Specifies compression Level (0 &lt;= x &lt;= 9).
        /// </summary>
        public Int32 Level { get => InternalProperties.Level; set => InternalProperties.Level = value; }

        /// <summary>
        /// <summary>
        /// Specifies number of fast bytes for LZ*.
        /// </summary>
        public UInt32 NumFastBytes { get => InternalProperties.NumFastBytes; set => InternalProperties.NumFastBytes = value; }

        /// <summary>
        /// Specifies match finder. (any of <see cref="MatchFinderType.HC3ZIP"/> / <see cref="MatchFinderType.BT3ZIP"/>)
        /// </summary>
        public MatchFinderType MatchFinder { get => InternalProperties.MatchFinder; set => InternalProperties.MatchFinder = value; }

        /// <summary>
        /// Specifies the number of match finder cyckes.
        /// </summary>
        public UInt32 MatchFinderCycles { get => InternalProperties.MatchFinderCycles; set => InternalProperties.MatchFinderCycles = value; }

        /// <summary>
        /// Specifies number of passes.
        /// </summary>
        public UInt32 NumPasses { get => InternalProperties.NumPasses; set => InternalProperties.NumPasses = value; }

        /// <summary>
        /// Specifies false for fast mode, true for normal mode.
        /// </summary>
        public bool Algorithm { get => InternalProperties.Algorithm; set => InternalProperties.Algorithm = value; }

        internal InternalDeflateEncoderProperties InternalProperties { get; }
    }
}
