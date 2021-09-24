namespace Utility
{
    /// <summary>
    /// <see cref="IReadOnlyArray{ELEMENT_T}"/>オブジェクトの内部表現を参照するインターフェースです。
    /// このインターフェースは、<see cref="IReadOnlyArray{ELEMENT_T}"/>はサポートしないが<see cref="ELEMENT_T[]"/>はサポートする
    /// メソッドのための互換性を確保するために用意されています。
    /// </summary>
    /// <typeparam name="ELEMENT_T">
    /// 配列の要素の型です。
    /// </typeparam>
    /// <remarks>
    /// すべての<see cref="IReadOnlyArray{ELEMENT_T}"/>オブジェクトが必ずしも<see cref="IReadOnlyArrayInternalObject<ELEMENT_T>"/>を
    /// 実装しているわけではないことに留意してください。
    /// </remarks>
    internal interface IReadOnlyArrayInternalObject<ELEMENT_T>
    {
        /// <summary>
        /// <see cref="IReadOnlyArray{ELEMENT_T}"/>オブジェクトの生の<see cref="ELEMENT_T"/>の配列を取得します。
        /// </summary>
        /// <remarks>
        /// このメソッドで得られた<see cref="ELEMENT_T"/>の配列のサイズおよび内容は決して変更しないでください。
        /// また、このメソッドで取得した配列のサイズまたは内容が変更されない保証がない場合には、このメソッドを決して使用しないでください。
        /// </remarks>
        ELEMENT_T[] InternalArray { get; }
    }
}
