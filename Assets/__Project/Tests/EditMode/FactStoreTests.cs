using System.Collections.Generic;
using Core.Logging;
using Narrative.Facts.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class FactStoreTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public readonly List<string> Warnings = new();
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) => Warnings.Add(message);
            public void Error(LogCategory category, string message) { }
        }

        /// <summary>Minimal registry that knows exactly the keys it is seeded with.</summary>
        private sealed class FakeRegistry : IFactKeyRegistry
        {
            private readonly Dictionary<(FactNamespace, string), FactKeyInfo> _infos = new();

            public FakeRegistry Add(FactNamespace ns, string key, FactValueType type)
            {
                _infos[(ns, key)] = new FactKeyInfo(ns, key, FactScope.Global, type, FactValue.DefaultFor(type));
                return this;
            }

            public bool TryGetInfo(FactNamespace ns, string key, out FactKeyInfo info)
            {
                return _infos.TryGetValue((ns, key), out info);
            }
        }

        private static FactKey World(string key) => FactKey.Global(FactNamespace.World, key);

        [Test]
        public void Set_ThenTryGet_ReturnsStoredValue()
        {
            var store = new FactStore();
            store.Set(World("pass_cleared"), FactValue.FromBool(true));

            Assert.IsTrue(store.TryGet(World("pass_cleared"), out var value));
            Assert.IsTrue(value.AsBool());
        }

        [Test]
        public void GetOrDefault_ReturnsFallback_WhenUnset_AndStored_WhenSet()
        {
            var store = new FactStore();
            var key = World("reputation");

            Assert.AreEqual(7, store.GetOrDefault(key, FactValue.FromInt(7)).AsInt());

            store.Set(key, FactValue.FromInt(3));
            Assert.AreEqual(3, store.GetOrDefault(key, FactValue.FromInt(7)).AsInt());
        }

        [Test]
        public void Has_IsPresenceOnly_AndIgnoresDefault()
        {
            // B4: an unset fact has a default value via GetOrDefault but does NOT "exist".
            var store = new FactStore();
            var key = World("burned");

            Assert.IsFalse(store.Has(key));
            Assert.IsFalse(store.GetOrDefault(key, FactValue.FromBool(false)).AsBool());

            store.Set(key, FactValue.FromBool(false));
            Assert.IsTrue(store.Has(key)); // present even though value equals the default
        }

        [Test]
        public void Remove_ClearsPresence_ButDefaultStillApplies()
        {
            var store = new FactStore();
            var key = World("flag");
            store.Set(key, FactValue.FromBool(true));

            Assert.IsTrue(store.Remove(key));
            Assert.IsFalse(store.Has(key));
            Assert.IsFalse(store.Remove(key)); // already gone
            Assert.IsTrue(store.GetOrDefault(key, FactValue.FromBool(true)).AsBool());
        }

        [Test]
        public void OnFactChanged_FiresOnSetAndRemove()
        {
            var store = new FactStore();
            var changes = new List<FactKey>();
            store.OnFactChanged += (k, v) => changes.Add(k);

            var key = World("x");
            store.Set(key, FactValue.FromInt(1));
            store.Remove(key);

            Assert.AreEqual(2, changes.Count);
            Assert.AreEqual(key, changes[0]);
            Assert.AreEqual(key, changes[1]);
        }

        [Test]
        public void Namespaces_AreIsolated_EvenWithSameKeyName()
        {
            var store = new FactStore();
            store.Set(new FactKey(FactNamespace.World, "", "reputation"), FactValue.FromInt(1));
            store.Set(new FactKey(FactNamespace.Faction, "blades", "reputation"), FactValue.FromInt(2));
            store.Set(new FactKey(FactNamespace.Actor, "npc_07", "reputation"), FactValue.FromInt(3));

            Assert.AreEqual(1, store.GetOrDefault(new FactKey(FactNamespace.World, "", "reputation"), FactValue.FromInt(0)).AsInt());
            Assert.AreEqual(2, store.GetOrDefault(new FactKey(FactNamespace.Faction, "blades", "reputation"), FactValue.FromInt(0)).AsInt());
            Assert.AreEqual(3, store.GetOrDefault(new FactKey(FactNamespace.Actor, "npc_07", "reputation"), FactValue.FromInt(0)).AsInt());
        }

        [Test]
        public void Snapshot_IsStablyOrdered_RegardlessOfInsertionOrder()
        {
            var a = new FactStore();
            a.Set(new FactKey(FactNamespace.Faction, "z", "k"), FactValue.FromInt(1));
            a.Set(new FactKey(FactNamespace.World, "", "b"), FactValue.FromInt(1));
            a.Set(new FactKey(FactNamespace.World, "", "a"), FactValue.FromInt(1));

            var b = new FactStore();
            b.Set(new FactKey(FactNamespace.World, "", "a"), FactValue.FromInt(1));
            b.Set(new FactKey(FactNamespace.Faction, "z", "k"), FactValue.FromInt(1));
            b.Set(new FactKey(FactNamespace.World, "", "b"), FactValue.FromInt(1));

            var sa = a.Snapshot();
            var sb = b.Snapshot();

            CollectionAssert.AreEqual(KeysOf(sa), KeysOf(sb));
            // World.a, World.b, Faction.z.k by stable order
            Assert.AreEqual("world.a", sa[0].Key.ToString());
            Assert.AreEqual("world.b", sa[1].Key.ToString());
            Assert.AreEqual("faction.z.k", sa[2].Key.ToString());
        }

        [Test]
        public void UnknownKey_FailsClosedAndWarns_WhenRegistryPresent()
        {
            var logger = new FakeLogger();
            var registry = new FakeRegistry().Add(FactNamespace.World, "known", FactValueType.Bool);
            var store = new FactStore(registry, logger);

            store.Set(World("unknown"), FactValue.FromBool(true));

            Assert.IsFalse(store.Has(World("unknown")));
            Assert.AreEqual(1, logger.Warnings.Count);
        }

        [Test]
        public void TypeMismatch_FailsClosedAndWarns()
        {
            var logger = new FakeLogger();
            var registry = new FakeRegistry().Add(FactNamespace.World, "count", FactValueType.Int);
            var store = new FactStore(registry, logger);

            store.Set(World("count"), FactValue.FromBool(true));

            Assert.IsFalse(store.Has(World("count")));
            Assert.AreEqual(1, logger.Warnings.Count);
        }

        [Test]
        public void FactValue_NumericTypesCompareAndCoerce()
        {
            Assert.IsTrue(FactValue.FromInt(2).Equals(FactValue.FromFloat(2d)));
            Assert.AreEqual(1, FactValue.FromInt(3).CompareNumericOrEquatable(FactValue.FromInt(2)));
            Assert.IsTrue(FactValue.FromBool(true).AsInt() == 1);
            Assert.IsFalse(FactValue.FromString("a").Equals(FactValue.FromInt(0)));
        }

        private static List<FactKey> KeysOf(IReadOnlyList<KeyValuePair<FactKey, FactValue>> entries)
        {
            var keys = new List<FactKey>(entries.Count);
            foreach (var e in entries)
            {
                keys.Add(e.Key);
            }

            return keys;
        }
    }
}
