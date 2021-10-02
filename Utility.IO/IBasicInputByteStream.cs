using System;

namespace Utility.IO
{
    public interface IBasicInputByteStream
        : IDisposable
    {
        /// <summary>
        /// バイトストリームから指定バイト数だけ読み込みます。
        /// </summary>
        /// <param name="buffer">
        /// 読み込んだデータを格納するためのバイト配列です。
        /// </param>
        /// <param name="offset">
        /// <paramref name="buffer"/>上の読み込み開始位置を示す<see cref="int"/>値です。
        /// </param>
        /// <param name="count">
        /// 読み込むバイト数を示す<see cref="int"/>値です。
        /// </param>
        /// <returns>
        /// <see cref="int"/>値を返します。
        /// 復帰値が正の値である場合、 それは実際に読み込まれたデータの長さ(バイト数)を示します。この値は<paramref name="count"/>を超えることはありません。
        /// 復帰値が0である場合、それはバイトストリームの終端に達したことを示します。
        /// </returns>
        int Read(byte[] buffer, int offset, int count);
    }
}
