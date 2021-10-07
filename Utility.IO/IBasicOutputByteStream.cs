using System;

namespace Utility.IO
{
    public interface IBasicOutputByteStream
        : IDisposable
    {
        /// <summary>
        /// バイト配列をバイトストリームに書き込みます。
        /// </summary>
        /// <param name="buffer">
        /// 書き込む配列です。
        /// </param>
        /// <param name="offset">
        /// <paramref name="buffer"/>上の書き込み開始位置を示す<see cref="int"/>値です。
        /// </param>
        /// <param name="count">
        /// 書き込むバイト数を示す<see cref="int"/>値です。
        /// </param>
        /// <returns>
        /// 実際に書き込むことができたバイト数を示す<see cref="int"/>値です。
        /// <paramref name="count"/>が正の値の場合は、必ず正の値を返します。
        /// </returns>
        /// <remarks>
        /// <para>
        /// 呼び出す場合の注意: このメソッドの仕様は、.NETの<see cref="System.IO.Stream.Write(byte[], int, int)"/>よりも、UNIX系のシステムコールの write に似ています。
        /// このメソッドは<paramref name="count"/>で与えられたバイト数のすべてを書き込むとは限りません。
        /// 必ず、実際に書き込めたバイト数を復帰値で確認して、復帰値が<paramref name="count"/>より小さかった場合は、続きのデータを書き込むために再度このメソッドを呼び出してください。
        /// </para>
        /// </remarks>
        int Write(IReadOnlyArray<byte> buffer, int offset, int count);

        /// <summary>
        /// 内部バッファのデータをすべてバイトストリームに書き出します。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 呼び出す場合の注意: 実装によっては、このメソッドは内部データのすべてを書き出さないこともあります。
        /// このメソッドが「必ず内部データのすべてを書き出す」ことを期待してはいけません。
        /// </para>
        /// <para>
        /// 実装する場合の注意: <see cref="Flush"/>をサポートする必要が特にない場合は何もせずに復帰するようにしてください。
        /// 常に<see cref="NotSupportedException"/>例外や<see cref="NotImplementedException"/>例外を発生させるような実装はしないでください。
        /// </para>
        /// </remarks>
        void Flush();

        /// <summary>
        /// バイトストリームを明示的に閉じる宣言をします。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 呼びだす場合の注意: このメソッドを呼び出した後は、<see cref="IDisposable.Dispose"/>以外のメソッドを呼び出してはいけません。
        /// </para>
        /// <para>
        /// 実装する場合の注意: このメソッドが必ず呼び出されることは期待しないでください。
        /// </para>
        /// </remarks>
        void Close();
    }
}
