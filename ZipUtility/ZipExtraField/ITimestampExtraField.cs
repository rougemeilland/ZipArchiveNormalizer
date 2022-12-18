using System;

namespace ZipUtility.ZipExtraField
{
    /// <summary>
    /// 一般的な日時(最終更新日時/最終アクセス日時/作成日時)を参照/設定可能な拡張フィールドのインターフェース。
    /// </summary>
    interface ITimestampExtraField
        : IExtraField
    {
        /// <summary>
        /// 最終更新日時を表す <see cref="DateTime"/> オブジェクト。
        /// 値が存在しない場合は null である。
        /// 値が存在する場合に参照できる <see cref="DateTime"/> オブジェクトの <see cref="DateTime.Kind"/> プロパティは <see cref="DateTimeKind.Utc"/> である。
        /// 設定可能な値は、null、または <see cref="DateTime.Kind"/> プロパティの値が <see cref="DateTimeKind.Utc"/> または <see cref="DateTimeKind.Local"/> である <see cref="DateTime"/> オブジェクトである。
        /// </summary>
        /// <remarks>
        /// 実装時の注意として、代入で与えられた <see cref="DateTime"/> オブジェクトの <see cref="DateTime.Kind"/> プロパティがもし <see cref="DateTimeKind.Local"/> であった場合は、 <see cref="DateTime.ToUniversalTime"/> メソッドを使用して UTC に変換して内部に保持するべきである。
        /// また、代入で与えられた値の <see cref="DateTime.Kind"/> プロパティが <see cref="DateTimeKind.Unspecified"/> であった場合は、代入を許可するべきではない。
        /// </remarks>
        DateTime? LastWriteTimeUtc { get; set; }

        /// <summary>
        /// 最終アクセス日時を表す <see cref="DateTime"/> オブジェクト。
        /// 値が存在しない場合は null である。
        /// 値が存在する場合に参照できる <see cref="DateTime"/> オブジェクトの <see cref="DateTime.Kind"/> プロパティは <see cref="DateTimeKind.Utc"/> である。
        /// 設定可能な値は、null、または <see cref="DateTime.Kind"/> プロパティの値が <see cref="DateTimeKind.Utc"/> または <see cref="DateTimeKind.Local"/> である <see cref="DateTime"/> オブジェクトである。
        /// </summary>
        /// <remarks>
        /// 実装時の注意として、代入で与えられた <see cref="DateTime"/> オブジェクトの <see cref="DateTime.Kind"/> プロパティがもし <see cref="DateTimeKind.Local"/> であった場合は、 <see cref="DateTime.ToUniversalTime"/> メソッドを使用して UTC に変換して内部に保持するべきである。
        /// また、代入で与えられた値の <see cref="DateTime.Kind"/> プロパティが <see cref="DateTimeKind.Unspecified"/> であった場合は、代入を許可するべきではない。
        /// </remarks>
        DateTime? LastAccessTimeUtc { get; set; }

        /// <summary>
        /// 作成日時を表す <see cref="DateTime"/> オブジェクト。
        /// 値が存在しない場合は null である。
        /// 値が存在する場合に参照できる <see cref="DateTime"/> オブジェクトの <see cref="DateTime.Kind"/> プロパティは <see cref="DateTimeKind.Utc"/> である。
        /// 設定可能な値は、null、または <see cref="DateTime.Kind"/> プロパティの値が <see cref="DateTimeKind.Utc"/> または <see cref="DateTimeKind.Local"/> である <see cref="DateTime"/> オブジェクトである。
        /// </summary>
        /// <remarks>
        /// 実装時の注意として、代入で与えられた <see cref="DateTime"/> オブジェクトの <see cref="DateTime.Kind"/> プロパティがもし <see cref="DateTimeKind.Local"/> であった場合は、 <see cref="DateTime.ToUniversalTime"/> メソッドを使用して UTC に変換して内部に保持するべきである。
        /// また、代入で与えられた値の <see cref="DateTime.Kind"/> プロパティが <see cref="DateTimeKind.Unspecified"/> であった場合は、代入を許可するべきではない。
        /// </remarks>
        DateTime? CreationTimeUtc { get; set; }
    }
}
