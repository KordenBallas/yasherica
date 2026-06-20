using System;
using System.Collections.Generic;
using Core.Logging;

namespace Narrative.Facts.Core
{
    /// <summary>
    /// Default in-memory <see cref="IFactStore"/>. A single dictionary backs every namespace so the
    /// machinery is uniform (R9). When an <see cref="IFactKeyRegistry"/> is supplied, writes to
    /// unknown or mistyped keys fail closed (skipped) and warn — surfacing authoring errors rather
    /// than silently corrupting run-state, mirroring the existing RunConditionEvaluator philosophy.
    /// A null registry makes the store permissive (used by low-level unit tests).
    /// </summary>
    public class FactStore : IFactStore
    {
        private readonly Dictionary<FactKey, FactValue> _facts = new Dictionary<FactKey, FactValue>();
        private readonly IFactKeyRegistry _registry;
        private readonly IGameLogger _logger;

        public event Action<FactKey, FactValue> OnFactChanged;

        public FactStore(IFactKeyRegistry registry = null, IGameLogger logger = null)
        {
            _registry = registry;
            _logger = logger;
        }

        public bool TryGet(FactKey key, out FactValue value)
        {
            return _facts.TryGetValue(key, out value);
        }

        public FactValue GetOrDefault(FactKey key, FactValue fallback)
        {
            return _facts.TryGetValue(key, out var value) ? value : fallback;
        }

        public bool Has(FactKey key)
        {
            return _facts.ContainsKey(key);
        }

        public void Set(FactKey key, FactValue value)
        {
            if (!Validate(key, value))
            {
                return;
            }

            _facts[key] = value;
            OnFactChanged?.Invoke(key, value);
        }

        public bool Remove(FactKey key)
        {
            if (!_facts.TryGetValue(key, out var removed))
            {
                return false;
            }

            _facts.Remove(key);
            OnFactChanged?.Invoke(key, removed);
            return true;
        }

        public IReadOnlyList<KeyValuePair<FactKey, FactValue>> Snapshot()
        {
            var entries = new List<KeyValuePair<FactKey, FactValue>>(_facts.Count);
            foreach (var pair in _facts)
            {
                entries.Add(pair);
            }

            entries.Sort((a, b) => FactKey.Comparer.Compare(a.Key, b.Key));
            return entries;
        }

        private bool Validate(FactKey key, FactValue value)
        {
            if (_registry == null)
            {
                return true;
            }

            if (!_registry.TryGetInfo(key.Namespace, key.Key, out var info))
            {
                _logger?.Warning($"[FactStore] Unknown fact key '{key}' - write rejected (fail closed).");
                return false;
            }

            if (info.ValueType != value.Type)
            {
                _logger?.Warning(
                    $"[FactStore] Type mismatch for fact '{key}': declared {info.ValueType}, got {value.Type} - write rejected.");
                return false;
            }

            return true;
        }
    }
}
