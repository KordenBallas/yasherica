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

        private static Unit UnitWith(int id, params IStatusEffect[] effects)
        {
            return new Unit(
                id: id,
                owner: null,
                position: new HexCoordinates(id, 0),
                currentHP: 100,
                maxHP: 100,
                abilities: null,
                statusEffects: effects);
        }

        private static IStatusEffect Modifier(
            StatusEffectType type, int magnitude, StatTarget target, int stackCount = 1)
        {
            return new DataDrivenModifierEffect(
                id: 7,
                name: "mod",
                type: type,
                duration: -1,
                stackCount: stackCount,
                stackRule: StackRule.StackToCap,
                magnitude: magnitude,
                target: target,
                triggerType: StatusEffectTriggerType.TurnEnd);
        }

        [Test]
        public void CalculateFinalDamage_NoModifiers_ReturnsBaseDamage()
        {
            var attacker = UnitWith(1);
            var target = UnitWith(2);

            Assert.AreEqual(BaseDamage, _damageSystem.CalculateFinalDamage(attacker, target, BaseDamage));
        }

        [Test]
        public void CalculateFinalDamage_OutgoingBuff_AddsFlatDamage()
        {
            var attacker = UnitWith(1, Modifier(StatusEffectType.Buff, 5, StatTarget.OutgoingDamage));
            var target = UnitWith(2);

            Assert.AreEqual(15, _damageSystem.CalculateFinalDamage(attacker, target, BaseDamage));
        }

        [Test]
        public void CalculateFinalDamage_OutgoingDebuff_SubtractsFlatDamage()
        {
            var attacker = UnitWith(1, Modifier(StatusEffectType.Debuff, -5, StatTarget.OutgoingDamage));
            var target = UnitWith(2);

            Assert.AreEqual(5, _damageSystem.CalculateFinalDamage(attacker, target, BaseDamage));
        }

        [Test]
        public void CalculateFinalDamage_TargetIncomingModifier_ReducesDamageTaken()
        {
            // Hardened: the TARGET's IncomingDamage modifier applies (the P3-13/S3 target side).
            var attacker = UnitWith(1);
            var target = UnitWith(2, Modifier(StatusEffectType.Buff, -5, StatTarget.IncomingDamage));

            Assert.AreEqual(5, _damageSystem.CalculateFinalDamage(attacker, target, BaseDamage));
        }

        [Test]
        public void CalculateFinalDamage_BothSides_Sum()
        {
            var attacker = UnitWith(1, Modifier(StatusEffectType.Buff, 5, StatTarget.OutgoingDamage));
            var target = UnitWith(2, Modifier(StatusEffectType.Buff, -5, StatTarget.IncomingDamage));

            Assert.AreEqual(BaseDamage, _damageSystem.CalculateFinalDamage(attacker, target, BaseDamage));
        }

        [Test]
        public void CalculateFinalDamage_AttackerIncomingModifier_DoesNotAffectOutgoingDamage()
        {
            // The attacker's own Hardened must not change what it DEALS.
            var attacker = UnitWith(1, Modifier(StatusEffectType.Buff, -5, StatTarget.IncomingDamage));
            var target = UnitWith(2);

            Assert.AreEqual(BaseDamage, _damageSystem.CalculateFinalDamage(attacker, target, BaseDamage));
        }

        [Test]
        public void CalculateFinalDamage_ScalesWithStackCount()
        {
            var attacker = UnitWith(1, Modifier(StatusEffectType.Buff, 3, StatTarget.OutgoingDamage, stackCount: 2));
            var target = UnitWith(2);

            Assert.AreEqual(16, _damageSystem.CalculateFinalDamage(attacker, target, BaseDamage));
        }

        [Test]
        public void CalculateFinalDamage_NeverGoesNegative()
        {
            var attacker = UnitWith(1, Modifier(StatusEffectType.Debuff, -25, StatTarget.OutgoingDamage));
            var target = UnitWith(2);

            Assert.AreEqual(0, _damageSystem.CalculateFinalDamage(attacker, target, BaseDamage));
        }

        [Test]
        public void CalculateFinalDamage_IgnoresNonModifierBuffEffects()
        {
            // A plain Buff-typed effect that is not a DataDrivenModifierEffect carries no stat change.
            var attacker = UnitWith(1, new StatusEffect(9, "flag", StatusEffectType.Buff, -1));
            var target = UnitWith(2);

            Assert.AreEqual(BaseDamage, _damageSystem.CalculateFinalDamage(attacker, target, BaseDamage));
        }

        [Test]
        public void CalculateFinalDamage_NullAttacker_ReturnsBaseDamage()
        {
            Assert.AreEqual(BaseDamage, _damageSystem.CalculateFinalDamage(null, null, BaseDamage));
        }

        [Test]
        public void CalculateFinalDamage_NullTarget_UsesAttackerSideOnly()
        {
            var attacker = UnitWith(1, Modifier(StatusEffectType.Buff, 5, StatTarget.OutgoingDamage));

            Assert.AreEqual(15, _damageSystem.CalculateFinalDamage(attacker, null, BaseDamage));
        }
    }
}
