using System;
using System.Collections.Generic;
using System.Linq;
using Utility;
using Utility.IO;

namespace ZipUtility.ZipExtraField
{
    public class ExtraFieldStorage
    {
        private class InternalExtraFieldItem
        {
            public InternalExtraFieldItem(UInt16 extraFieldId, ZipEntryHeaderType appliedHeaderType, ReadOnlyMemory<byte> extraFieldBody)
            {
                ExtraFieldId = extraFieldId;
                AppliedHeaderType = appliedHeaderType;
                ExtraFieldBody = extraFieldBody;
            }

            public UInt16 ExtraFieldId { get; }
            public ZipEntryHeaderType AppliedHeaderType { get; }
            public ReadOnlyMemory<byte> ExtraFieldBody { get; }
        }


        private readonly ZipEntryHeaderType _headerType;
        private readonly IDictionary<UInt16, InternalExtraFieldItem> _extraFields;

        /// <summary>
        /// 指定されたヘッダの拡張フィールドを保持し、拡張フィールドの初期値のデータソースとして空のデータが与えられたコンストラクタ
        /// </summary>
        /// <param name="headerType">
        /// 対応するヘッダのIDを表す <see cref="ZipEntryHeaderType"/> 値
        /// </param>
        public ExtraFieldStorage(ZipEntryHeaderType headerType)
            : this(headerType, new Dictionary<UInt16, InternalExtraFieldItem>())
        {
        }

        /// <summary>
        /// 指定された <see cref="ExtraFieldStorage"/> オブジェクトを複製するコンストラクタ
        /// </summary>
        /// <param name="source">
        /// 複製元の <see cref="ExtraFieldStorage"/> オブジェクト
        /// </param>
        public ExtraFieldStorage(ExtraFieldStorage source)
            : this(source._headerType, source._extraFields.ToDictionary(item => item.Key, item => item.Value))
        {
        }

        /// <summary>
        /// 指定されたヘッダの拡張フィールドを保持し、拡張フィールドの初期値のデータソースとしてバイトシーケンスが与えられたコンストラクタ
        /// </summary>
        /// <param name="headerType">
        /// 対応するヘッダのIDを表す <see cref="ZipEntryHeaderType"/> 値
        /// </param>
        /// <param name="extraFieldsSource">
        /// 拡張フィールドの初期値のデータソースとして与えられた <see cref="ReadOnlyMemory{byte}">ReadOnlyMemory&lt;<see cref="byte"/>&gt;</see> オブジェクト。
        /// </param>
        public ExtraFieldStorage(ZipEntryHeaderType headerType, ReadOnlyMemory<byte> extraFieldsSource)
            : this(headerType, new Dictionary<UInt16, InternalExtraFieldItem>())
        {
            AppendExtraFields(extraFieldsSource);
        }

        /// <summary>
        /// 指定されたヘッダの拡張フィールドを保持し、拡張フィールドの初期値のデータソースとして <see cref="ExtraFieldStorage"/> オブジェクトと <see cref="ReadOnlyMemory{byte}">ReadOnlyMemory&lt;<see cref="byte"/>&gt;</see> が与えられたコンストラクタ
        /// </summary>
        /// <param name="headerType">
        /// 対応するヘッダのIDを表す <see cref="ZipEntryHeaderType"/> 値
        /// </param>
        /// <param name="source">
        /// 拡張フィールドの初期値のデータソースとして与えられた <see cref="ExtraFieldStorage"/> オブジェクト。
        /// </param>
        /// <param name="additionalExtraFieldsSource">
        /// 拡張フィールドの初期値のデータソースとして与えられた <see cref="ReadOnlyMemory{byte}">ReadOnlyMemory&lt;<see cref="byte"/>&gt;</see> オブジェクト。
        /// </param>
        /// <remarks>
        /// source で与えられた拡張フィールドと同じIDの拡張フィールドが additionalExtraFieldsSource にも存在していた場合は、additionalExtraFieldsSource の内容が優先される。
        /// また、source に存在していて additionalExtraFieldsSource に存在していない拡張フィールドを <see cref="GetData{EXTRA_FIELD_T}">GetData&lt;<typeparamref name="EXTRA_FIELD_T"/>&gt;</see> メソッドで取得した場合、
        /// その際に適用されるヘッダの種類は headerType ではなく source で与えられた <see cref="ExtraFieldStorage"/> オブジェクトに依存する。
        /// </remarks>
        public ExtraFieldStorage(ZipEntryHeaderType headerType, ExtraFieldStorage source, ReadOnlyMemory<byte> additionalExtraFieldsSource)
            : this(headerType, source._extraFields.ToDictionary(item => item.Key, item => item.Value))
        {
            AppendExtraFields(additionalExtraFieldsSource);
        }

        private ExtraFieldStorage(ZipEntryHeaderType headerType, IDictionary<UInt16, InternalExtraFieldItem> extraFields)
        {
            _headerType = headerType;
            _extraFields = extraFields;
        }

        /// <summary>
        /// 拡張フィールドを追加する
        /// </summary>
        /// <typeparam name="EXTRA_FIELD_T">
        /// 追加する拡張フィールドのクラス。このクラスは <see cref="IExtraField"/> を実装している必要がある。
        /// </typeparam>
        /// <param name="extraField">
        /// 追加する拡張フィールドのオブジェクト。
        /// </param>
        public void AddEntry<EXTRA_FIELD_T>(EXTRA_FIELD_T extraField)
            where EXTRA_FIELD_T : IExtraField
        {
            var body = extraField.GetData(_headerType);
            if (!body.HasValue)
                return;
            if (body.Value.Length > UInt16.MaxValue)
                throw new OverflowException(string.Format("Too large extra field data in {0}", extraField.GetType().FullName));
            _extraFields[extraField.ExtraFieldId] = new InternalExtraFieldItem(extraField.ExtraFieldId, _headerType, body.Value);
        }

        /// <summary>
        /// 拡張フィールドのコレクションを消去する。
        /// </summary>
        public void Clear()
        {
            _extraFields.Clear();
        }

        /// <summary>
        /// 拡張フィールドのコレクションから、指定されたIDの拡張フィールドを削除する。
        /// </summary>
        /// <param name="extraFieldId">
        /// 削除する拡張フィールドのID
        /// </param>
        public void Delete(UInt16 extraFieldId)
        {
            _extraFields.Remove(extraFieldId);
        }

        /// <summary>
        /// 拡張フィールドのコレクションに、指定されたIDの拡張フィールドが含まれているかどうかを調べる。
        /// </summary>
        /// <param name="extraFieldId">
        /// コレクションに含まれているかどうかを調べる対象の拡張フィールドID
        /// </param>
        /// <returns>
        /// 指定されたIDの拡張フィールドがコレクションに含まれていれば true 、そうではないなら false 。
        /// </returns>
        public bool Contains(UInt16 extraFieldId)
        {
            return _extraFields.ContainsKey(extraFieldId);
        }

        /// <summary>
        /// 拡張フィールドのクラスを指定することにより、コレクションから拡張フィールドを取得する。
        /// </summary>
        /// <typeparam name="EXTRA_FIELD_T">
        /// コレクションから取得したい拡張フィールドのクラス
        /// </typeparam>
        /// <returns>
        /// EXTRA_FIELD_T 型パラメタで与えられた拡張フィールドがコレクションに含まれていればそのオブジェクトを返す。
        /// 含まれていなかった場合は null を返す。
        /// </returns>
        public EXTRA_FIELD_T? GetData<EXTRA_FIELD_T>()
            where EXTRA_FIELD_T : class, IExtraField, new()
        {
            var extraField = new EXTRA_FIELD_T();
            if (!_extraFields.TryGetValue(extraField.ExtraFieldId, out InternalExtraFieldItem? sourceData))
                return null;
            extraField.SetData(sourceData.AppliedHeaderType, sourceData.ExtraFieldBody);
            return extraField;
        }

        /// <summary>
        /// 拡張フィールドのコレクションのバイト配列表現を表すバイトシーケンスを返す。
        /// </summary>
        /// <returns>
        /// 拡張フィールドのコレクションのバイト配列表現を表す <see cref="ReadOnlyMemory{Byte}">ReadOnlyMemory&lt;<see cref="Byte"/>&gt;</see> オブジェクト。
        /// </returns>
        public ReadOnlyMemory<Byte> ToByteArray()
        {
            var writer = new ByteArrayRenderer();
            foreach (var extraFieldItem in _extraFields.Values)
            {
                writer.WriteUInt16LE(extraFieldItem.ExtraFieldId);
                writer.WriteUInt16LE((UInt16)extraFieldItem.ExtraFieldBody.Length);
                writer.WriteBytes(extraFieldItem.ExtraFieldBody);
            }
            return writer.ToByteArray();
        }

        /// <summary>
        /// 拡張フィールドのコレクションに含まれる拡張フィールドのIDのシーケンスを取得する。
        /// </summary>
        /// <returns>
        /// 拡張フィールドのIDのシーケンスを表す <see cref="IEnumerable{UInt16}">IEnumerable&lt;<see cref="UInt16"/>&gt;</see> オブジェクト。
        /// </returns>
        public IEnumerable<UInt16> EnumerateExtraFieldIds()
        {
            return _extraFields.Keys.ToList();
        }

        /// <summary>
        /// コレクションに含まれている拡張属性の種類の数
        /// </summary>
        public Int32 Count => _extraFields.Count;

        private void AppendExtraFields(ReadOnlyMemory<byte> extraFieldsSource)
        {
            var reader = new ByteArrayParser(extraFieldsSource);
            while (!reader.IsEmpty)
            {
                try
                {
                    var extraFieldId = reader.ReadUInt16LE();
                    var extraFieldBodyLength = reader.ReadUInt16LE();
                    var extraFieldBody = reader.ReadBytes(extraFieldBodyLength);
                    _extraFields[extraFieldId] = new InternalExtraFieldItem(extraFieldId, _headerType, extraFieldBody);
                }
                catch (UnexpectedEndOfStreamException ex)
                {
                    throw
                        new BadZipFileFormatException(
                            string.Format(
                                "Can not parse extra fields: header='{0}', extra data='{1}'",
                                _headerType,
                                extraFieldsSource.ToArray().ToFriendlyString()),
                            ex);
                }
            }
        }
    }
}
