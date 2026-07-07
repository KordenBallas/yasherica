using System.Collections.Generic;
using System.Reflection;
using Combat.Core;
using Combat.Data;
using Combat.Data.Definitions;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// The shared "Applies: ..." grammar (FR9): one formatter feeds the ability-preview
    /// popover and the mutation card, so every surface names a status identically.
    /// </summary>
    [TestFixture]
    public class StatusPreviewTextTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in _created)
                Object.DestroyImmediate(asset);
            _created.Clear();
        }

        private static void SetPrivate(object target, string field, object value)
        {
            var info = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(info, $"Field '{field}' not found on {target.GetType().Name}.");
            info.SetValue(target, value);
        }

        private StatusEffectDefinition NewStatus(string name, int duration)
        {
            var def = ScriptableObject.CreateInstance<StatusEffectDefinition>();
            _created.Add(def);
            SetPrivate(def, "_id", 1);
            SetPrivate(def, "_name", name);
            SetPrivate(def, "_duration", duration);
            SetPrivate(def, "_type", StatusEffectType.DamageOverTime);
            return def;
        }

        private T NewAbility<T>(StatusEffectDefinition status, int durationOverride = -1)
            where T : AbilityDefinition
        {
            var def = ScriptableObject.CreateInstance<T>();
            _created.Add(def);
            SetPrivate(def, "_statusEffect", status);
            SetPrivate(def, "_durationOverride", durationOverride);
            return def;
        }

        [Test]
        public void StatusAbility_DescribesNameAndDuration()
        {
            var ability = NewAbility<StatusEffectAbilityDefinition>(NewStatus("Burn", 2));

            Assert.IsTrue(StatusEffectPreviewText.TryDescribeApplied(ability, out var text));
            Assert.AreEqual("Applies: Burn (2 turns)", text);
        }

        [Test]
        public void SingleTurn_UsesSingular()
        {
            var ability = NewAbility<HybridAbilityDefinition>(NewStatus("Stun", 1));

            Assert.IsTrue(StatusEffectPreviewText.TryDescribeApplied(ability, out var text));
            Assert.AreEqual("Applies: Stun (1 turn)", text);
        }

        [Test]
        public void DurationOverride_WinsOverTheStatusDefault()
        {
            var ability = NewAbility<StatusEffectAbilityDefinition>(NewStatus("Burn", 2), durationOverride: 4);

            Assert.IsTrue(StatusEffectPreviewText.TryDescribeApplied(ability, out var text));
            Assert.AreEqual("Applies: Burn (4 turns)", text);
        }

        [Test]
        public void PlainDamageAbility_HasNoStatusLine()
        {
            var def = ScriptableObject.CreateInstance<DamageAbilityDefinition>();
            _created.Add(def);

            Assert.IsFalse(StatusEffectPreviewText.TryDescribeApplied(def, out _));
        }

        [Test]
        public void Passive_DescribesTheStandingModifier()
        {
            var passive = ScriptableObject.CreateInstance<PassiveAbilityDefinition>();
            _created.Add(passive);
            SetPrivate(passive, "_modifier", NewStatus("Hardened", -1));

            Assert.IsTrue(StatusEffectPreviewText.TryDescribeStanding(passive, out var text));
            Assert.AreEqual("Standing: Hardened", text);
        }
    }
}
