using System.Collections.Generic;

namespace Narrative.Facts.Core
{
    /// <summary>
    /// Immutable, UnityEngine-free implementation of the authored fact vocabulary. Built once from
    /// the <c>FactKeyRegistry</c> ScriptableObject (via <c>FactKeyRegistryMapper</c>) and shared as
    /// the single source of truth (R13). Lookups are by (namespace, bare key).
    /// </summary>
    public sealed class FactKeyRegistry : IFactKeyRegistry
    {
        private readonly Dictionary<(FactNamespace, string), FactKeyInfo> _byKey;

        public FactKeyRegistry(IEnumerable<FactKeyInfo> keys)
        {
            _byKey = new Dictionary<(FactNamespace, string), FactKeyInfo>();
            if (keys == null)
            {
                return;
            }

            foreach (var info in keys)
            {
                if (string.IsNullOrEmpty(info.Key))
                {
                    continue;
                }

                _byKey[(info.Namespace, info.Key)] = info;
            }
        }

        public bool TryGetInfo(FactNamespace ns, string key, out FactKeyInfo info)
        {
            return _byKey.TryGetValue((ns, key ?? string.Empty), out info);
        }

        /// <summary>All declared keys (used by the typed-accessor drift check, D3).</summary>
        public IReadOnlyCollection<FactKeyInfo> Keys => _byKey.Values;
    }
}
