﻿using System;

namespace ZipUtility.ZipExtraField
{
    /// <summary>
    /// 拡張フィールドのインターフェース。
    /// <see cref="IExtraField"/>の一つのクラスの実装では一つのIDの拡張フィールドしか扱えないため、
    /// 必要な拡張フィールドの種類の分だけクラスを実装する必要がある。
    /// </summary>
    public interface IExtraField
    {
        /// <summary>
        /// 拡張フィールドのIDを表す、読み取り専用の <see cref="UInt16"/> の整数
        /// </summary>
        UInt16 ExtraFieldId { get; }

        /// <summary>
        /// 拡張フィールドを表すバイト配列を返すメソッド。
        /// </summary>
        /// <param name="headerType">
        /// バイト配列を構築する際に適用されるべきヘッダの種類を表すID
        /// </param>
        /// <returns>
        /// 構築されたバイト配列を返す。
        /// </returns>
        /// <remarks>
        /// このメソッドは、拡張フィールドのコレクションに拡張フィールドを追加しようとしたときに呼び出される。
        /// このメソッドを実装の際、もし何らかの理由でコレクションに拡張フィールドを追加してほしくない場合は null を返してもよい。
        /// </remarks>
        byte[] GetData(ZipEntryHeaderType headerType);

        /// <summary>
        /// バイト配列から拡張フィールドを解析するメソッド。
        /// </summary>
        /// <param name="headerType">
        /// バイト配列を解析する際に適用されるべきヘッダの種類を表すID
        /// </param>
        /// <param name="data">
        /// 解析対象のバイト配列。
        /// </param>
        /// <param name="offset">
        /// 解析対象のバイト配列のうち、参照可能な範囲の開始場所。
        /// </param>
        /// <param name="count">
        /// 解析対象のバイト配列のうち、参照可能な範囲の長さ。
        /// </param>
        /// <remarks>
        /// 拡張フィールドのコレクションから特定の拡張フィールドのオブジェクトを取得しようとしたときに呼び出される。
        /// data で与えられたバイト配列のうち、参照が許可されているのはインデックスが offset 以上、かつ offset + count 未満である。
        /// それ以外の場所を参照した場合の動作は未定義である。
        /// </remarks>
        void SetData(ZipEntryHeaderType headerType, byte[] data, int offset, int count);
    }
}
