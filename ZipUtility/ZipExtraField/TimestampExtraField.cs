using System;

namespace ZipUtility.ZipExtraField
{
    public abstract class TimestampExtraField
        : ExtraField, ITimestampExtraField
    {
        private DateTime? _lastWriteTimeUtc;
        private DateTime? _lastAccessTimeUtc;
        private DateTime? _creationTimeUtc;

        protected TimestampExtraField(UInt16 extraFieldId)
            : base(extraFieldId)
        {
            _lastWriteTimeUtc = null;
            _lastAccessTimeUtc = null;
            _creationTimeUtc = null;
        }

        public override abstract ReadOnlyMemory<byte>? GetData(ZipEntryHeaderType headerType);
        public override abstract void SetData(ZipEntryHeaderType headerType, ReadOnlyMemory<byte> data);

        public virtual DateTime? LastWriteTimeUtc
        {
            get => _lastWriteTimeUtc;

            set =>
                _lastWriteTimeUtc =
                    value is null || value.Value.Kind != DateTimeKind.Unspecified
                    ? value?.ToUniversalTime()
                    : throw new ArgumentException($"Unexpected {nameof(DateTime.Kind)} value", nameof(value));
        }

        public virtual DateTime? LastAccessTimeUtc
        {
            get => _lastAccessTimeUtc;

            set =>
                _lastAccessTimeUtc =
                    value is null || value.Value.Kind != DateTimeKind.Unspecified
                    ? value?.ToUniversalTime()
                    : throw new ArgumentException($"Unexpected {nameof(DateTime.Kind)} value", nameof(value));
        }

        public virtual DateTime? CreationTimeUtc
        {
            get => _creationTimeUtc;

            set =>
                _creationTimeUtc =
                    value is null || value.Value.Kind != DateTimeKind.Unspecified
                    ? value?.ToUniversalTime()
                    : throw new ArgumentException($"Unexpected {nameof(DateTime.Kind)} value", nameof(value));
        }
    }
}
