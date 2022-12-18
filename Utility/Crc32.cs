using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility
{
    public static class Crc32
    {
        public static ICrcCalculationState<UInt32, UInt64> CreateCalculationState() => ByteArrayExtensions.CreateCrc32CalculationState();
    }
}
