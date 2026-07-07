using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Core.StatusEffects;
using Combat.Execution;
using Combat.Player;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// FR5 stack rules through the real application path (AbilityExecutor — the single seam
    /// PvE player queues, PvE enemies, and Arena commits all ride): Refresh resets the clock,
    /// StackToCap accumulates to its cap and still refreshes, Ignore is a no-op.
    /// </summary>
    [TestFixture]
    public class StatusEffectStackRuleTests
    {
        private const int CasterId = 1;
        private const int VictimId = 2;
        private const int AbilityId = 100;
        private const int FreshDuration = 3;

        private HexDirectionConfig _config;
        private HumanPlayer _player;
        private HumanPlayer _enemyOwner;
        private AbilityExecutor _executor;

        [SetUp]
        public void SetUp()
        {
            _config = TestHexDirectionConfig.CreateFlatTop();
            _player = new HumanPlayer(1, "Player");
            _enemyOwner = new HumanPlayer(2, "Enemies");
            var damage = new DamageSystem();
            _executor = new AbilityExecutor(
                damage, new StatusEffectTriggerProcessor(damage), new AbilityShapeCalculator(_config), _config);
        }

        [TearDown]
        public void TearDown()
        {
            TestHexDirectionConfig.Destroy(_config);
        }

        private static IStatusEffect Dot(StackRule rule, int duration = FreshDuration,
            int stackCount = 1, int maxStacks = 0, int damagePerTrigger = 4, int damagePerStack = 4)
        {
            return new DataDrivenDamageOverTimeEffect(
                id: 50, name: "Toxin", duration: duration, stackCount: stackCount,
                stackRule: rule, maxStacks: maxStacks,
                triggerType: StatusEffectTriggerType.TurnEnd,
                damagePerTrigger: damagePerTrigger, damagePerStack: damagePerStack);
        }

        private static IAbilityInstance StatusAbility(IStatusEffect effect)
        {
            return new AbilityInstance(new DataDrivenStatusEffectAbility(
                AbilityId, "Test Status", 0, AbilityShapeData.ForLine(1), effect, effect.Duration));
        }

        private CombatState BuildState(Unit caster, IStatusEffect existingOnVictim)
        {
            var victim = new Unit(VictimId, _enemyOwner, new HexCoordinates(1, 0), 50, 50,
                new List<IAbilityInstance>(),
                statusEffects: existingOnVictim != null
                    ? new List<IStatusEffect> { existingOnVictim }
                    : null);

            return new CombatState(
                new List<IUnit> { caster, victim },
                new List<IPlayer> { _player, _enemyOwner },
                currentPlayer: _player,
                phase: CombatPhase.Combat);
        }

        private IUnit ApplyTo(IStatusEffect existingOnVictim, IStatusEffect applied)
        {
            var ability = StatusAbility(applied);
            var caster = new Unit(CasterId, _player, new HexCoordinates(0, 0), 50, 50,
                    new List<IAbilityInstance> { ability },
                    new List<ScheduledAbility> { new ScheduledAbility(ability, 0) })
                .WithFacingDirection(HexDirection.E);
            var state = BuildState(caster, existingOnVictim);

            var newState = _executor.ExecuteAbility(state, caster, caster.AbilityQueue[0]);
            return newState.GetUnit(VictimId);
        }

        [Test]
        public void FirstApplication_AddsTheEffectAtFreshDuration()
        {
            var victim = ApplyTo(existingOnVictim: null, applied: Dot(StackRule.Refresh));

            var effect = victim.StatusEffects.Single();
            Assert.AreEqual(FreshDuration, effect.Duration);
            Assert.AreEqual(1, effect.StackCount);
        }

        [Test]
        public void Refresh_ReApplication_ResetsDurationWithoutStacking()
        {
            // The victim already carries the toxin, nearly expired.
            var victim = ApplyTo(
                existingOnVictim: Dot(StackRule.Refresh, duration: 1),
                applied: Dot(StackRule.Refresh));

            var effect = victim.StatusEffects.Single();
            Assert.AreEqual(FreshDuration, effect.Duration, "refresh resets the clock");
            Assert.AreEqual(1, effect.StackCount, "refresh never stacks");
        }

        [Test]
        public void StackToCap_ReApplication_AddsAStackAndRefreshesDuration()
        {
            var victim = ApplyTo(
                existingOnVictim: Dot(StackRule.StackToCap, duration: 1, maxStacks: 3),
                applied: Dot(StackRule.StackToCap, maxStacks: 3));

            var effect = (ITriggeredStatusEffect)victim.StatusEffects.Single();
            Assert.AreEqual(2, ((IStatusEffect)effect).StackCount);
            Assert.AreEqual(FreshDuration, ((IStatusEffect)effect).Duration, "stacking also refreshes");
            Assert.AreEqual(8, effect.DamagePerTrigger, "each stack adds its per-stack damage (4 -> 8)");
        }

        [Test]
        public void StackToCap_AtTheCap_KeepsStacksButStillRefreshesDuration()
        {
            var victim = ApplyTo(
                existingOnVictim: Dot(StackRule.StackToCap, duration: 1, stackCount: 3, maxStacks: 3),
                applied: Dot(StackRule.StackToCap, maxStacks: 3));

            var effect = victim.StatusEffects.Single();
            Assert.AreEqual(3, effect.StackCount, "capped at max stacks");
            Assert.AreEqual(FreshDuration, effect.Duration, "re-application at cap still resets the clock");
        }

        [Test]
        public void Ignore_ReApplication_LeavesTheExistingEffectUntouched()
        {
            var victim = ApplyTo(
                existingOnVictim: Dot(StackRule.Ignore, duration: 1),
                applied: Dot(StackRule.Ignore));

            var effect = victim.StatusEffects.Single();
            Assert.AreEqual(1, effect.Duration, "ignore keeps the running clock (no chain-lock)");
            Assert.AreEqual(1, effect.StackCount);
        }
    }
}
