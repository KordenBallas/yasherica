using System;
using System.Collections.Generic;
using Narrative.Facts.Core;

namespace Narrative.Runtime.Snapshots
{
    /// <summary>One serialized fact (primitive fields only, JsonUtility/Newtonsoft-friendly).</summary>
    [Serializable]
    public class FactEntryDto
    {
        public FactNamespace Namespace;
        public string Subject;
        public string Key;
        public FactValueType Type;
        public bool BoolValue;
        public long IntValue;
        public double FloatValue;
        public string StringValue;
    }

    /// <summary>Serializable image of the whole fact store, emitted in stable key order (W2-5).</summary>
    [Serializable]
    public class FactStoreSnapshot
    {
        public List<FactEntryDto> Entries = new List<FactEntryDto>();
    }

    /// <summary>
    /// Pure bridge between <see cref="IFactStore"/> and its serializable snapshot. Capture uses the
    /// store's stable-ordered <see cref="IFactStore.Snapshot"/> so saves are reproducible; restore
    /// replays each entry through <see cref="IFactStore.Set"/> (re-validated against the registry).
    /// </summary>
    public static class FactStoreSnapshotMapper
    {
        public static FactStoreSnapshot Capture(IFactStore store)
        {
            var snapshot = new FactStoreSnapshot();
            if (store == null)
            {
                return snapshot;
            }

            foreach (var pair in store.Snapshot())
            {
                snapshot.Entries.Add(ToDto(pair.Key, pair.Value));
            }

            return snapshot;
        }

        /// <summary>
        /// Captures only the facts on one lifetime horizon (D20), so the run/meta partition handed to
        /// save/load is already clean. A key missing from the vocabulary counts as run-scoped — the
        /// conservative side: a mistagged fact is lost on death rather than leaking across runs.
        /// </summary>
        public static FactStoreSnapshot Capture(IFactStore store, IFactKeyRegistry registry, FactHorizon horizon)
        {
            var snapshot = new FactStoreSnapshot();
            if (store == null)
            {
                return snapshot;
            }

            foreach (var pair in store.Snapshot())
            {
                if (HorizonOf(pair.Key, registry) != horizon)
                {
                    continue;
                }

                snapshot.Entries.Add(ToDto(pair.Key, pair.Value));
            }

            return snapshot;
        }

        private static FactHorizon HorizonOf(FactKey key, IFactKeyRegistry registry)
        {
            return registry != null && registry.TryGetInfo(key.Namespace, key.Key, out var info)
                ? info.Horizon
                : FactHorizon.Run;
        }

        public static void Restore(FactStoreSnapshot snapshot, IFactStore store)
        {
            if (snapshot == null || store == null)
            {
                return;
            }

            foreach (var entry in snapshot.Entries)
            {
                store.Set(new FactKey(entry.Namespace, entry.Subject, entry.Key), ToValue(entry));
            }
        }

        private static FactEntryDto ToDto(FactKey key, FactValue value)
        {
            return new FactEntryDto
            {
                Namespace = key.Namespace,
                Subject = key.Subject,
                Key = key.Key,
                Type = value.Type,
                BoolValue = value.Type == FactValueType.Bool && value.AsBool(),
                IntValue = value.Type == FactValueType.Int ? value.AsInt() : 0,
                FloatValue = value.Type == FactValueType.Float ? value.AsFloat() : 0d,
                StringValue = value.Type == FactValueType.String ? value.AsString() : string.Empty
            };
        }

        private static FactValue ToValue(FactEntryDto entry)
        {
            switch (entry.Type)
            {
                case FactValueType.Bool: return FactValue.FromBool(entry.BoolValue);
                case FactValueType.Int: return FactValue.FromInt(entry.IntValue);
                case FactValueType.Float: return FactValue.FromFloat(entry.FloatValue);
                case FactValueType.String: return FactValue.FromString(entry.StringValue);
                default: return FactValue.FromBool(entry.BoolValue);
            }
        }
    }
}
