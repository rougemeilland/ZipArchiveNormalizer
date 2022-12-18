using System;
using System.Collections.Generic;
using Utility;

namespace SevenZip.Compression
{
    public abstract class CoderProperties
    {
        protected interface INormalizedPropertiesSource
        {
            bool IsSet(Enum propertyId);
            object this[Enum propertyId] { get; set; }
        }

        private class NormalizedPropertiesSource
            : INormalizedPropertiesSource
        {
            private readonly CoderProperties _properties;

            public NormalizedPropertiesSource(CoderProperties properties)
            {
                _properties = properties;
            }

            object INormalizedPropertiesSource.this[Enum propertyId]
            {
                get => _properties._propertiesCache[propertyId];

                set
                {
                    if (!_properties.Normalizer.IsValidValue(_properties._normalizedPropertiesSource, propertyId, value))
                        throw new InternalLogicalErrorException();
                    _properties._propertiesCache[propertyId] = value;
                }
            }

            bool INormalizedPropertiesSource.IsSet(Enum propertyId)
            {
                return _properties._propertiesCache.ContainsKey(propertyId);
            }
        }

        protected interface IPropertiesNormalizer
        {
            bool IsValidValue(INormalizedPropertiesSource properties, Enum propertyId, object value);
            void Normalize(INormalizedPropertiesSource properties);
        }

        private readonly IDictionary<Enum, object> _properties;
        private readonly IDictionary<Enum, object> _propertiesCache;
        private readonly INormalizedPropertiesSource _normalizedPropertiesSource;
        private bool _normalized;

        protected CoderProperties()
        {
            _properties = new Dictionary<Enum, object>();
            _propertiesCache = new Dictionary<Enum, object>();
            _normalized = false;
            _normalizedPropertiesSource = new NormalizedPropertiesSource(this);
        }

        protected object GetValue(Enum propertyId)
        {
            if (!_normalized)
            {
                _propertiesCache.Clear();
                foreach (var property in _properties)
                    _propertiesCache.Add(property.Key, property.Value);
                Normalizer.Normalize(_normalizedPropertiesSource);
            }
            if (!_propertiesCache.TryGetValue(propertyId, out object? value))
                throw new InvalidOperationException("value is not set");
            return value;
        }

        protected void SetValue(Enum propertyId, object value)
        {
            if (!Normalizer.IsValidValue(_normalizedPropertiesSource, propertyId, value))
                throw new ArgumentException($"Property '{propertyId}' value is invalid.", propertyId.ToString());
            _properties[propertyId] = value;
            _normalized = false;
        }

        protected abstract IPropertiesNormalizer Normalizer { get; }
    }
}
