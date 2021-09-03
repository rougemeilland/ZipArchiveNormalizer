using System;
using System.Collections.Generic;

namespace Utility
{
    /// <summary>
    /// デリゲートによって与えられた比較方法により <see cref="IEqualityComparer{T}"/>を実装するクラスです。
    /// </summary>
    /// <typeparam name="VALUE_T"></typeparam>
    public class CustomizableEqualityComparer<VALUE_T>
        : IEqualityComparer<VALUE_T>
    {
        private Func<VALUE_T, VALUE_T, bool> _equalityComparer;
        private Func<VALUE_T, int> _hashCalculater;

        /// <summary>
        /// 値の比較方法を示すデリゲートによって初期化されるコンストラクタです。
        /// </summary>
        /// <param name="equalityComparer">
        /// 与えられた値が等値かどうかを返すデリゲートです。
        /// このデリゲートは、二つの<see cref="VALUE_T"/>値をパラメタで受け取り、それらの値が等値かどうかを示す<see cref="bool"/>値を返す必要があります。
        /// 第一パラメタの値と第二パラメタの値が等値である場合は true、そうではない場合は false を返す必要があります。
        /// 型パラメタ <see cref="VALUE_T"/> がクラスである場合でも、 このデリゲートのパラメタに null が与えられることはありません。
        /// </param>
        /// <param name="hashCalculater">
        /// 与えられた値のハッシュコードを返すデリゲートです。
        /// このデリゲート、は一つの<see cref="VALUE_T"/>値をパラメタで受け取り、そのハッシュコードを示す<see cref="int"/>値を返す必要があります。
        /// 型パラメタ<see cref="VALUE_T"/>がクラスである場合でも、 このデリゲートのパラメタに null が与えられることはありません。
        /// </param>
        public CustomizableEqualityComparer(Func<VALUE_T, VALUE_T, bool> equalityComparer, Func<VALUE_T, int> hashCalculater)
        {
            _equalityComparer = equalityComparer;
            _hashCalculater = hashCalculater;
        }

        public bool Equals(VALUE_T x, VALUE_T y)
        {
            if (x == null)
                return y == null;
            else if (y == null)
                return false;
            else
                return _equalityComparer(x, y);
        }

        public int GetHashCode(VALUE_T obj)
        {
            return obj == null ? 0 : _hashCalculater(obj);
        }
    }
}