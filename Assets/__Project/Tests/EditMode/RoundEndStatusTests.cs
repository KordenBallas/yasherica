using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Core;
using Combat.Core.StatusEffects;
using Combat.Execution;
using Combat.Player;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// The one deterministic status resolve point (FR3/FR4): at round end a DoT deals its
    /// damage, then durations count down, then expired effects drop — via the same
    /// RoundLifecycleProcessor PvE and Arena both call. DoT ticks are raw (no modifiers),
    /// HoT clamps at max HP, infinite part-passives never tick away.
    /// </summary>
    [TestFixture]
    public class RoundEndStatusTests
    {
        private const int UnitId = 1;
        private const int MaxHp = 50;

        private HumanPlayer _player;
        private DamageSystem _damage;
        private RoundLifecycleProcessor _lifecycle;

        [SetUp]
        public void SetUp()
        {
            _player = new HumanPlayer(1, "Player");
            _damage = new DamageSystem();
            _lifecycle = new RoundLifecycleProcessor(new StatusEffectTriggerProcessor(_damage));
        }

        private CombatState StateWith(int hp, params IStatusEffect[] effects)
        {
            var unit = new Unit(UnitId, _player, new HexCoordinates(0, 0), hp, MaxHp,
                new List<IAbilityInstance>(), statusEffects: effects);
            return new CombatState(
                new List<IUnit> { unit },
                new List<IPlayer> { _player },
                currentPlayer: _player,
                phase: CombatPhase.Combat);
        }

        private static IStatusEffect Burn(int duration, int damagePerTrigger = 8)
        {
            return new DataDrivenDamageOverTimeEffect(
                1, "Burn", duration, 1, StackRule.Refresh, 0,
                StatusEffectTriggerType.TurnEnd, damagePerTrigger, 0);
        }

        private static IStatusEffect Regen(int duration, int healPerTrigger = 5)
        {
            return new DataDrivenHealOverTimeEffect(
                10, "Regen", duration, 1, StackRule.Refresh, 0,
                StatusEffectTriggerType.TurnEnd, healPerTrigger, 0);
        }

        [Test]
        public void RoundEnd_DoTDealsItsDamage_ThenDurationCountsDown()
        {
            var state = _lifecycle.ApplyRoundEndEffects(StateWith(MaxHp, Burn(duration: 2)));

            var unit = state.GetUnit(UnitId);
            Assert.AreEqual(MaxHp - 8, unit.CurrentHP, "the tick landed");
            Assert.AreEqual(1, unit.StatusEffects.Single().Duration, "then the clock ticked");
        }

        [Test]
        public void RoundEnd_ExpiryIsVisible_TheEffectDropsOffTheUnit()
        {
            var state = _lifecycle.ApplyRoundEndEffects(StateWith(MaxHp, Burn(duration: 1)));

            var unit = state.GetUnit(UnitId);
            Assert.AreEqual(MaxHp - 8, unit.CurrentHP, "the last tick still lands");
            CollectionAssert.IsEmpty(unit.StatusEffects, "expired at zero and removed");
        }

        [Test]
        public void RoundEnd_DoTOverTwoRounds_TotalsItsFullDamage()
        {
            var state = (ICombatState)StateWith(MaxHp, Burn(duration: 2));
            state = _lifecycle.ApplyRoundEndEffects(state);
            state = _lifecycle.ApplyRoundEndEffects(state);

            var unit = state.GetUnit(UnitId);
            Assert.AreEqual(MaxHp - 16, unit.CurrentHP, "8 + 8 over the two rounds");
            CollectionAssert.IsEmpty(unit.StatusEffects);
        }

        [Test]
        public void RoundEnd_DoTTickIsRaw_TheVictimsIncomingModifierDoesNotSoftenIt()
        {
            // Hardened is a hit modifier (CalculateFinalDamage); a DoT tick applies raw damage
            // by design — the condition already "landed" when it was applied.
            var hardened = new DataDrivenModifierEffect(
                9, "Hardened", StatusEffectType.Buff, -1, 1, StackRule.Refresh,
                -5, StatTarget.IncomingDamage, StatusEffectTriggerType.TurnEnd);

            var state = _lifecycle.ApplyRoundEndEffects(StateWith(MaxHp, Burn(duration: 2), hardened));

            Assert.AreEqual(MaxHp - 8, state.GetUnit(UnitId).CurrentHP, "full 8, not 3");
        }

        [Test]
        public void RoundEnd_HoTHeals_AndClampsAtMaxHp()
        {
            var state = _lifecycle.ApplyRoundEndEffects(StateWith(MaxHp - 3, Regen(duration: 3)));

            Assert.AreEqual(MaxHp, state.GetUnit(UnitId).CurrentHP, "healed 3 of the 5, clamped");
        }

        [Test]
        public void RoundEnd_InfiniteDurationPassive_PersistsAndNeverCountsDown()
        {
            var standing = new DataDrivenModifierEffect(
                20, "Empowered", StatusEffectType.Buff, -1, 1, StackRule.Refresh,
                5, StatTarget.OutgoingDamage, StatusEffectTriggerType.TurnEnd);

            var state = (ICombatState)StateWith(MaxHp, standing);
            for (int round = 0; round < 4; round++)
                state = _lifecycle.ApplyRoundEndEffects(state);

            var effect = state.GetUnit(UnitId).StatusEffects.Single();
            Assert.AreEqual(-1, effect.Duration, "the part passive is the duration-less case (FR10)");
        }

        [Test]
        public void RoundEnd_DoTCanKill()
        {
            var state = _lifecycle.ApplyRoundEndEffects(StateWith(5, Burn(duration: 2)));

            var unit = state.GetUnit(UnitId);
            Assert.AreEqual(0, unit.CurrentHP);
            Assert.IsFalse(unit.IsAlive, "the round-end tick decides the fight");
        }
    }
}
