using System;
using System.Collections.Generic;

namespace Narrative.Facts.Core
{
    /// <summary>
    /// Address of a single fact in the unified store: a namespace, an optional subject (the concrete
    /// entity id for scoped facts — actor instance id, faction id, location id — or empty for global
    /// facts), and a bare key. Value type with structural equality so it can key a dictionary.
    ///
    /// <see cref="Comparer"/> gives a stable total order (Ns, Subject, Key) so snapshots and any
    /// iteration that feeds a decision are reproducible (raw <see cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>
    /// order is not guaranteed).
    /// </summary>
    public readonly struct FactKey : IEquatable<FactKey>
    {
        public FactNamespace Namespace { get; }

        /// <summary>Concrete subject id for scoped facts; empty string for global facts.</summary>
        public string Subject { get; }

        public string Key { get; }

        public FactKey(FactNamespace ns, string subject, string key)
        {
            Namespace = ns;
            Subject = subject ?? string.Empty;
            Key = key ?? string.Empty;
        }

        public static FactKey Global(FactNamespace ns, string key) => new FactKey(ns, string.Empty, key);

        public bool Equals(FactKey other)
        {
            return Namespace == other.Namespace
                   && string.Equals(Subject, other.Subject, StringComparison.Ordinal)
                   && string.Equals(Key, other.Key, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is FactKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Namespace;
                hash = (hash * 397) ^ Subject.GetHashCode();
                hash = (hash * 397) ^ Key.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            var ns = Namespace.ToString().ToLowerInvariant();
            return string.IsNullOrEmpty(Subject) ? $"{ns}.{Key}" : $"{ns}.{Subject}.{Key}";
        }

        /// <summary>Stable total order: namespace, then subject (ordinal), then key (ordinal).</summary>
        public static readonly IComparer<FactKey> Comparer = new FactKeyComparer();

        private sealed class FactKeyComparer : IComparer<FactKey>
        {
            public int Compare(FactKey x, FactKey y)
            {
                int ns = x.Namespace.CompareTo(y.Namespace);
                if (ns != 0)
                {
                    return ns;
                }

                int subject = string.CompareOrdinal(x.Subject, y.Subject);
                if (subject != 0)
                {
                    return subject;
                }

                return string.CompareOrdinal(x.Key, y.Key);
            }
        }
    }
}
