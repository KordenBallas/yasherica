using System.Collections.Generic;
using System.Reflection;
using CharacterSystem.Data;
using CharacterSystem.Data.Definitions;
using Combat.Data.Definitions;
using Combat.Integration;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class PartAbilityResolverTests
    {
        private sealed class FakePartCatalog : IPartCatalog
        {
            private readonly Dictionary<string, PartDefinition> _parts =
                new Dictionary<string, PartDefinition>();

            public FakePartCatalog With(string partId, PartDefinition part)
            {
                _parts[partId] = part;
                return this;
            }

            public IReadOnlyList<PartDefinition> All => new List<PartDefinition>(_parts.Values);

            public bool TryGet(string partId, out PartDefinition definition)
            {
                return _parts.TryGetValue(partId, out definition);
            }
        }

        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in _created)
            {
                Object.DestroyImmediate(asset);
            }

            _created.Clear();
        }

        private static void SetPrivate(object target, string field, object value)
        {
            var info = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(info, $"Field '{field}' not found on {target.GetType().Name}.");
            info.SetValue(target, value);
        }

        private AbilityDefinition NewActive()
        {
            var ability = ScriptableObject.CreateInstance<AbilityDefinition>();
            _created.Add(ability);
            return ability;
        }

        private PassiveAbilityDefinition NewPassive()
        {
            var passive = ScriptableObject.CreateInstance<PassiveAbilityDefinition>();
            _created.Add(passive);
            return passive;
        }

        private PartDefinition NewPart(
            List<AbilityDefinition> active = null,
            List<PassiveAbilityDefinition> passive = null)
        {
            var part = ScriptableObject.CreateInstance<PartDefinition>();
            _created.Add(part);
            SetPrivate(part, "_activeAbilities", active ?? new List<AbilityDefinition>());
            SetPrivate(part, "_passiveAbilities", passive ?? new List<PassiveAbilityDefinition>());
            return part;
        }

        [Test]
        public void Resolve_CollectsActiveAndPassiveAcrossParts()
        {
            var active = NewActive();
            var passive = NewPassive();
            var catalog = new FakePartCatalog()
                .With("part.head", NewPart(active: new List<AbilityDefinition> { active }))
                .With("part.tail", NewPart(passive: new List<PassiveAbilityDefinition> { passive }));
            var resolver = new PartAbilityResolver(catalog, null);

            var result = resolver.Resolve(new[] { "part.head", "part.tail" });

            CollectionAssert.AreEqual(new[] { active }, result.ActiveAbilities);
            CollectionAssert.AreEqual(new[] { passive }, result.PassiveAbilities);
        }

        [Test]
        public void Resolve_DedupesAbilitySharedByTwoParts()
        {
            var shared = NewActive();
            var catalog = new FakePartCatalog()
                .With("part.a", NewPart(active: new List<AbilityDefinition> { shared }))
                .With("part.b", NewPart(active: new List<AbilityDefinition> { shared }));
            var resolver = new PartAbilityResolver(catalog, null);

            var result = resolver.Resolve(new[] { "part.a", "part.b" });

            Assert.AreEqual(1, result.ActiveAbilities.Count);
        }

        [Test]
        public void Resolve_SkipsUnknownPartIdsAndNullEntries()
        {
            var active = NewActive();
            var catalog = new FakePartCatalog()
                .With("part.known", NewPart(active: new List<AbilityDefinition> { active }));
            var resolver = new PartAbilityResolver(catalog, null);

            var result = resolver.Resolve(new[] { "part.unknown", null, "", "part.known" });

            CollectionAssert.AreEqual(new[] { active }, result.ActiveAbilities);
        }

        [Test]
        public void Resolve_NullInput_ReturnsEmpty()
        {
            var resolver = new PartAbilityResolver(new FakePartCatalog(), null);

            var result = resolver.Resolve(null);

            Assert.IsTrue(result.IsEmpty);
        }
    }
}
