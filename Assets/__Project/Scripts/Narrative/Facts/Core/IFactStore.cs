using System;
using System.Collections.Generic;

namespace Narrative.Facts.Core
{
    /// <summary>
    /// The one unified fact store spanning the world/actor/faction namespaces (Narrative R6/R9).
    /// Preconditions read it; effects write it; the director and debug view observe it. Pure C#.
    ///
    /// Presence-vs-default contract (B4): <see cref="GetOrDefault"/> returns the supplied fallback
    /// when a key is unset, so comparison ops let the registry default participate; <see cref="Has"/>
    /// is presence-only and ignores defaults — these two must not be conflated.
    /// </summary>
    public interface IFactStore
    {
        bool TryGet(FactKey key, out FactValue value);

        /// <summary>The stored value, or <paramref name="fallback"/> when the key is unset.</summary>
        FactValue GetOrDefault(FactKey key, FactValue fallback);

        /// <summary>True only if a value is currently stored for the key (default ignored).</summary>
        bool Has(FactKey key);

        void Set(FactKey key, FactValue value);

        /// <summary>Removes a stored value if present; returns true if a value was removed.</summary>
        bool Remove(FactKey key);

        /// <summary>All stored facts in the stable <see cref="FactKey.Comparer"/> order (reproducible).</summary>
        IReadOnlyList<KeyValuePair<FactKey, FactValue>> Snapshot();

        /// <summary>Raised after any value is set, changed, or removed (removed → value is the removed value).</summary>
        event Action<FactKey, FactValue> OnFactChanged;
    }
}
