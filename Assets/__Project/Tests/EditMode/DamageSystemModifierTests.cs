using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Core;
using Combat.Core.StatusEffects;
using Combat.Execution;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class DamageSystemModifierTests
    {
        private const int BaseDamage = 10;
        private readonly DamageSystem _damageSystem = new DamageSystem();

        private static Unit UnitWith(params IStatusEffect[] effects)
        {
            return new Unit(
                id: 1,
                owner: null,
                position: new HexCoordinates(0, 0),
                currentHP: 100,
                maxHP: 100,
                abilities: null,
                statusEffects: effects);
        }

        private static IStatusEffect Modifier(StatusEffectType type, float statModifier, int stackCount = 1)
        {
            return new DataDrivenModifierEffect(
                id: 7,
                name: "mod",
                type: type,
                duration: -1,
                stackCount: stackCount,
                isStackable: true,
                statModifier: statModifier,
                triggerType: StatusEffectTriggerType.TurnStart);
        }

        [Test]
        public void CalculateFinalDamage_NoModifiers_ReturnsBaseDamage()
        {
            var attacker = UnitWith();

            Assert.AreEqual(BaseDamage, _damageSystem.CalculateFinalDamage(attacker, attacker, BaseDamage));
        }

        [Test]
        public void CalculateFinalDamage_Buff_IncreasesDamage()
        {
            var attacker = UnitWith(Modifier(StatusEffectType.Buff, 0.5f));

            // 10 * (1 + 0.5) = 15
            Assert.AreEqual(15, _damageSystem.CalculateFinalDamage(attacker, attacker, BaseDamage));
        }

        [Test]
        public void CalculateFinalDamage_Debuff_DecreasesDamage()
        {
            var attacker = UnitWith(Modifier(StatusEffectType.Debuff, -0.5f));

            // 10 * (1 - 0.5) = 5
            Assert.AreEqual(5, _damageSystem.CalculateFinalDamage(attacker, attacker, BaseDamage));
        }

        [Test]
        public void CalculateFinalDamage_ScalesWithStackCount()
        {
            var attacker = UnitWith(Modifier(StatusEffectType.Buff, 0.25f, stackCount: 2));

            // 10 * (1 + 0.25 * 2) = 15
            Assert.AreEqual(15, _damageSystem.CalculateFinalDamage(attacker, attacker, BaseDamage));
        }

        [Test]
        public void CalculateFinalDamage_IgnoresNonModifierBuffEffects()
        {
            // A plain Buff-typed effect that is not a DataDrivenModifierEffect carries no stat change.
            var attacker = UnitWith(new StatusEffect(9, "flag", StatusEffectType.Buff, -1));

            Assert.AreEqual(BaseDamage, _damageSystem.CalculateFinalDamage(attacker, attacker, BaseDamage));
        }

        [Test]
        public void CalculateFinalDamage_NullAttacker_ReturnsBaseDamage()
        {
            Assert.AreEqual(BaseDamage, _damageSystem.CalculateFinalDamage(null, null, BaseDamage));
        }
    }
}
