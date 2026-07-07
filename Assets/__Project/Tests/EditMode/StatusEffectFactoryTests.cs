using System.Collections.Generic;
using System.Reflection;
using Combat.Core;
using Combat.Core.StatusEffects;
using Combat.Data.Definitions;
using Combat.Data.Factories;
using Core.Logging;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// SO -> Core mapping for the status model: kind routing, the signed flat magnitude +
    /// stat target (no Buff/Debuff sign magic), the control kind/penalty, the stack rule —
    /// and the AbilityFactory duration-override fix (the override must reach the instance
    /// that is actually APPLIED, not just the EffectDuration display field).
    /// </summary>
    [TestFixture]
    public class StatusEffectFactoryTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private readonly List<Object> _created = new List<Object>();
        private readonly StatusEffectFactory _factory = new StatusEffectFactory();

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in _created)
                Object.DestroyImmediate(asset);
            _created.Clear();
        }

        private static void SetPrivate(object target, string field, object value)
        {
            // Walk the hierarchy: private fields declared on a base class (e.g. _id on
            // AbilityDefinition) are invisible to GetField on the subclass type.
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var info = type.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
                if (info != null)
                {
                    info.SetValue(target, value);
                    return;
                }
            }

            Assert.Fail($"Field '{field}' not found on {target.GetType().Name} or its bases.");
        }

        private StatusEffectDefinition NewStatus(
            StatusEffectType type, int duration = 2, StackRule stackRule = StackRule.Refresh,
            int maxStacks = 0, int damagePerTrigger = 0, int healPerTrigger = 0,
            ControlKind controlKind = ControlKind.Stun, int movementPenalty = 1,
            StatTarget statTarget = StatTarget.OutgoingDamage, int magnitude = 0)
        {
            var def = ScriptableObject.CreateInstance<StatusEffectDefinition>();
            _created.Add(def);
            SetPrivate(def, "_id", 42);
            SetPrivate(def, "_name", "Test Status");
            SetPrivate(def, "_type", type);
            SetPrivate(def, "_duration", duration);
            SetPrivate(def, "_stackRule", stackRule);
            SetPrivate(def, "_maxStacks", maxStacks);
            SetPrivate(def, "_triggerType", StatusEffectTriggerType.TurnEnd);
            SetPrivate(def, "_damagePerTrigger", damagePerTrigger);
            SetPrivate(def, "_healPerTrigger", healPerTrigger);
            SetPrivate(def, "_controlKind", controlKind);
            SetPrivate(def, "_movementPenalty", movementPenalty);
            SetPrivate(def, "_statTarget", statTarget);
            SetPrivate(def, "_magnitude", magnitude);
            return def;
        }

        [Test]
        public void DamageOverTime_MapsToDotRecord()
        {
            var effect = _factory.CreateStatusEffect(
                NewStatus(StatusEffectType.DamageOverTime, damagePerTrigger: 8));

            var dot = (DataDrivenDamageOverTimeEffect)effect;
            Assert.AreEqual(8, dot.DamagePerTrigger);
            Assert.AreEqual(2, dot.Duration);
            Assert.AreEqual(StackRule.Refresh, dot.StackRule);
        }

        [Test]
        public void Control_CarriesKindAndMovementPenalty()
        {
            var effect = _factory.CreateStatusEffect(
                NewStatus(StatusEffectType.Control, controlKind: ControlKind.Slow, movementPenalty: 2));

            var control = (DataDrivenControlEffect)effect;
            Assert.AreEqual(ControlKind.Slow, control.Kind);
            Assert.AreEqual(2, control.MovementPenalty);
        }

        [Test]
        public void Modifier_KeepsTheAuthoredSign_NoDebuffNegation()
        {
            // Hardened is a BUFF with a NEGATIVE incoming-damage magnitude — the factory must
            // never flip signs by type.
            var buff = _factory.CreateStatusEffect(NewStatus(
                StatusEffectType.Buff, statTarget: StatTarget.IncomingDamage, magnitude: -5));
            var debuff = _factory.CreateStatusEffect(NewStatus(
                StatusEffectType.Debuff, statTarget: StatTarget.OutgoingDamage, magnitude: -5));

            Assert.AreEqual(-5, ((DataDrivenModifierEffect)buff).Magnitude);
            Assert.AreEqual(StatTarget.IncomingDamage, ((DataDrivenModifierEffect)buff).Target);
            Assert.AreEqual(-5, ((DataDrivenModifierEffect)debuff).Magnitude);
        }

        [Test]
        public void StackRule_AndCap_AreThreadedThrough()
        {
            var effect = _factory.CreateStatusEffect(NewStatus(
                StatusEffectType.DamageOverTime, stackRule: StackRule.StackToCap, maxStacks: 3,
                damagePerTrigger: 4));

            Assert.AreEqual(StackRule.StackToCap, effect.StackRule);
            Assert.AreEqual(3, ((DataDrivenDamageOverTimeEffect)effect).MaxStacks);
        }

        [Test]
        public void DurationOverride_ReachesTheAppliedEffectInstance()
        {
            // The D7 fix: the ability's override duration must land on EffectToApply itself.
            var status = NewStatus(StatusEffectType.DamageOverTime, duration: 3, damagePerTrigger: 8);
            var abilityDef = ScriptableObject.CreateInstance<StatusEffectAbilityDefinition>();
            _created.Add(abilityDef);
            SetPrivate(abilityDef, "_id", 7);
            SetPrivate(abilityDef, "_name", "Test Apply");
            SetPrivate(abilityDef, "_statusEffect", status);
            SetPrivate(abilityDef, "_durationOverride", 5);

            var ability = (IStatusEffectAbility)new AbilityFactory(_factory, new FakeLogger())
                .CreateAbility(abilityDef);

            Assert.AreEqual(5, ability.EffectDuration);
            Assert.AreEqual(5, ability.EffectToApply.Duration, "the APPLIED instance carries the override");
        }

        [Test]
        public void NoOverride_UsesTheStatusDefaultDuration()
        {
            var status = NewStatus(StatusEffectType.DamageOverTime, duration: 3, damagePerTrigger: 8);
            var abilityDef = ScriptableObject.CreateInstance<StatusEffectAbilityDefinition>();
            _created.Add(abilityDef);
            SetPrivate(abilityDef, "_id", 7);
            SetPrivate(abilityDef, "_name", "Test Apply");
            SetPrivate(abilityDef, "_statusEffect", status);
            SetPrivate(abilityDef, "_durationOverride", -1);

            var ability = (IStatusEffectAbility)new AbilityFactory(_factory, new FakeLogger())
                .CreateAbility(abilityDef);

            Assert.AreEqual(3, ability.EffectToApply.Duration);
        }
    }
}
