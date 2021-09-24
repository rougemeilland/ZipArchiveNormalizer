using System;
using System.Collections.Generic;

namespace Utility
{
    public interface IReadOnlyArray<ELEMENT_T>
        : IEnumerable<ELEMENT_T>
    {
        int Length { get; }
        void CopyTo(ELEMENT_T[] destinationArray, int destinationOffset);
        void CopyTo(int sourceOffset, ELEMENT_T[] destinationArray, int destinationOffset, int count);
        ELEMENT_T this[int index] { get; }
        ELEMENT_T[] DuplicateAsWritableArray();

        [Obsolete]
        ELEMENT_T[] ToArray();
    }
}
