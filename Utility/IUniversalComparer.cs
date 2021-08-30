using System.Collections.Generic;

namespace Utility
{
    public interface IUniversalComparer<VALUE_T>
        : IEqualityComparer<VALUE_T>, IComparer<VALUE_T>
    {
    }
}