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
        int Write(IReadOnlyArray<byte> buffer, int offset, int count);

        /// <summary>
        /// 内部バッファのデータをすべてバイトストリームに書き出します。
        /// </summary>
        void Flush();

        /// <summary>
        /// バイトストリームを明示的に閉じます。
        /// </summary>
        void Close();
    }
}
