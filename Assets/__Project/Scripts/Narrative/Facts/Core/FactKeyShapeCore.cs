using System;

namespace Narrative.Facts.Core
{
    /// <summary>
    /// Immutable, UnityEngine-free permitted-write *target*: namespace + unresolved subject token +
    /// key + value type. A fragment's footprint (W2-1) is a set of these; the applier admits a
    /// play-time write only if its shape <see cref="Permits"/> match is in that set (W3-3/W4-1).
    /// Comparison is by namespace + key + the **unresolved subject token** (not arity) — so
    /// <c>actor.$self.hostile</c> permits a <c>$self</c> write but not a <c>$target</c> write.
    /// <c>op</c>/<c>value</c> play no part.
    /// </summary>
    public readonly struct FactKeyShapeCore : IEquatable<FactKeyShapeCore>
    {
        public FactNamespace Namespace { get; }
        public string SubjectToken { get; }
        public string Key { get; }
        public FactValueType ValueType { get; }

        public FactKeyShapeCore(FactNamespace ns, string subjectToken, string key, FactValueType valueType)
        {
            Namespace = ns;
            SubjectToken = subjectToken ?? string.Empty;
            Key = key ?? string.Empty;
            ValueType = valueType;
        }

        /// <summary>
        /// True if this declared shape permits a write of the candidate shape. Matches on
        /// namespace + key + unresolved subject token; value type must also agree.
        /// </summary>
        public bool Permits(FactKeyShapeCore write)
        {
            return Namespace == write.Namespace
                   && ValueType == write.ValueType
                   && string.Equals(Key, write.Key, StringComparison.Ordinal)
                   && string.Equals(SubjectToken, write.SubjectToken, StringComparison.Ordinal);
        }

        public bool Equals(FactKeyShapeCore other) => Permits(other) && other.Permits(this);

        public override bool Equals(object obj) => obj is FactKeyShapeCore other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Namespace;
                hash = (hash * 397) ^ (int)ValueType;
                hash = (hash * 397) ^ SubjectToken.GetHashCode();
                hash = (hash * 397) ^ Key.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            var ns = Namespace.ToString().ToLowerInvariant();
            return string.IsNullOrEmpty(SubjectToken) ? $"{ns}.{Key}:{ValueType}" : $"{ns}.{SubjectToken}.{Key}:{ValueType}";
        }
    }
}
