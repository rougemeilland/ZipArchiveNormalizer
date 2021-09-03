using System;
using System.Collections.Generic;

namespace Utility
{
    /// <summary>
    /// デリゲートによって与えられた比較方法によって <see cref="IComparer{T}"/> を実装するクラスです。
    /// </summary>
    /// <typeparam name="VALUE_T">
    /// </typeparam>
    public class CustomizableComparer<VALUE_T>
        : IComparer<VALUE_T>
    {
        private Func<VALUE_T, VALUE_T, int> _comparer;

        /// <summary>
        /// 値の比較方法を示すデリゲートによって初期化されるコンストラクタです。
        /// </summary>
        /// <param name="comparer">
        /// 与えられた値の大小関係を示す整数を返すデリゲートです。
        /// このデリゲートは、二つの<see cref="VALUE_T"/> 値をパラメタで受け取る必要があります。
        /// また、その大小関係を示す<see cref="int"/>値を返す必要があります。
        /// 第一パラメタの値が第二パラメタの値より大きい場合は正の整数、
        /// 第一パラメタの値が第二パラメタの値より小さい場合は負の整数、
        /// 第一パラメタの値が第二パラメタの値と等しい場合は0を返す必要があります。
        /// 型パラメタ <see cref="VALUE_T"/> がクラスである場合でも、 このデリゲートのパラメタに null が与えられることはありません。
        /// </param>
        public CustomizableComparer(Func<VALUE_T, VALUE_T, int> comparer)
        {
            _comparer = comparer;
        }

        public int Compare(VALUE_T x, VALUE_T y)
        {
            if (x == null)
                return y == null ? 0 : -1;
            else if (y == null)
                return 1;
            else
                return _comparer(x, y);
        }
    }
}