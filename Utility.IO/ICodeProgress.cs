using System;

namespace Utility.IO
{
    public interface ICodeProgress
    {
        /// <summary>
        /// Callback progress.
        /// </summary>
        /// <param name="inSize">
        /// input size. null if unknown.
        /// </param>
        /// <param name="outSize">
        /// output size. null if unknown.
        /// </param>
        void SetProgress(Int64? inSize, Int64? outSize);
    };
}
