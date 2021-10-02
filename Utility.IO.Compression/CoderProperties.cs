using System;
using System.Collections.Generic;

namespace Utility.IO.Compression
{
    public class CoderProperties
    {
        private IDictionary<CoderPropId, object> _properties;

        public CoderProperties()
        {
            _properties = new Dictionary<CoderPropId, object>();
        }

        private object GetValue(CoderPropId propertyId)
        {
            object value;
            if (!_properties.TryGetValue(propertyId, out value))
                throw new ArgumentException();
            return value;
        }

        private void SetValue(CoderPropId propertyId, object value)
        {
            _properties[propertyId] = value;
        }

        public UInt32 DictionarySize { get => (UInt32)GetValue(CoderPropId.DictionarySize); set => SetValue(CoderPropId.DictionarySize, value); }
        //public UInt32 UsedMemorySize { get => (UInt32)GetValue(CoderPropId.UsedMemorySize); set => SetValue(CoderPropId.UsedMemorySize, value); }
        //public UInt32 Order { get => (UInt32)GetValue(CoderPropId.Order); set => SetValue(CoderPropId.Order, value); }
        //public UInt64 BlockSize { get => (UInt64)GetValue(CoderPropId.BlockSize); set => SetValue(CoderPropId.BlockSize, value); }
        public Int32 PosStateBits { get => (Int32)GetValue(CoderPropId.PosStateBits); set => SetValue(CoderPropId.PosStateBits, value); }
        public Int32 LitContextBits { get => (Int32)GetValue(CoderPropId.LitContextBits); set => SetValue(CoderPropId.LitContextBits, value); }
        public Int32 LitPosBits { get => (Int32)GetValue(CoderPropId.LitPosBits); set => SetValue(CoderPropId.LitPosBits, value); }
        public UInt32 NumFastBytes { get => (UInt32)GetValue(CoderPropId.NumFastBytes); set => SetValue(CoderPropId.NumFastBytes, value); }
        public string MatchFinder { get => (string)GetValue(CoderPropId.MatchFinder); set => SetValue(CoderPropId.MatchFinder, value); }
        //public UInt32 MatchFinderCycles { get => (UInt32)GetValue(CoderPropId.MatchFinderCycles); set => SetValue(CoderPropId.MatchFinderCycles, value); }
        //public UInt32 NumPasses { get => (UInt32)GetValue(CoderPropId.NumPasses); set => SetValue(CoderPropId.NumPasses, value); }
        public UInt32 Algorithm { get => (UInt32)GetValue(CoderPropId.Algorithm); set => SetValue(CoderPropId.Algorithm, value); }
        //public UInt32 NumThreads { get => (UInt32)GetValue(CoderPropId.NumThreads); set => SetValue(CoderPropId.NumThreads, value); }
        public bool EndMarker { get => (bool)GetValue(CoderPropId.EndMarker); set => SetValue(CoderPropId.EndMarker, value); }
    }
}
