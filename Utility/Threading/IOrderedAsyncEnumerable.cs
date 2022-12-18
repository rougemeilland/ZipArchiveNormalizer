using System.Collections.Generic;

namespace Utility.Threading
{
    public interface IOrderedAsyncEnumerable<ELEMENT_T>
        : IAsyncEnumerable<ELEMENT_T>
    {
        IComparer<ELEMENT_T> GetComparer();
    }
}
