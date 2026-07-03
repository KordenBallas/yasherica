using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Execution;
using Combat.Player;
using Core.Logging;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Proves the facing model: directional abilities fire along the caster's CURRENT facing
    /// at execution time (turning re-points the whole queued volley), Ring ignores facing.
    /// </summary>
    [TestFixture]
    public class AbilityExecutorFacingTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private const int CasterId = 1;
        private const int VictimEastId = 2;
        private const int VictimWestId = 3;
        private const int DamageAbilityId = 100;
        private const int BaseDamage = 10;

        private HexDirectionConfig _config;
        private HumanPlayer _player;
        private HumanPlayer _enemyOwner;
        private AbilityExecutor _abilityExecutor;
        private ActionExecutor _actionExecutor;

        [SetUp]
        public void SetUp()
        {
            _config = TestHexDirectionConfig.CreateFlatTop();
            _player = new HumanPlayer(1, "Player");
            _enemyOwner = new HumanPlayer(2, "Enemies");

            var shapeCalculator = new AbilityShapeCalculator(_config);
            _abilityExecutor = new AbilityExecutor(new DamageSystem(), null, shapeCalculator, _config);
            _actionExecutor = new ActionExecutor(_abilityExecutor, new FakeLogger());
        }

        [TearDown]
        public void TearDown()
        {
            TestHexDirectionConfig.Destroy(_config);
        }

        private static IAbilityInstance LineDamageAbility()
        {
            return new AbilityInstance(new DataDrivenDamageAbility(
                DamageAbilityId, "Test Line", 1, AbilityShapeData.ForLine(2), BaseDamage));
        }

        private static IAbilityInstance RingDamageAbility()
        {
            return new AbilityInstance(new DataDrivenDamageAbility(
                DamageAbilityId, "Test Ring", 1, AbilityShapeData.ForRing(1), BaseDamage));
        }

        private CombatState BuildState(Unit caster)
        {
            // Victims one step east (1,0) and one step west (-1,0) of the caster at origin.
            var victimEast = new Unit(VictimEastId, _enemyOwner, new HexCoordinates(1, 0), 50, 50,
                new List<IAbilityInstance>());
            var victimWest = new Unit(VictimWestId, _enemyOwner, new HexCoordinates(-1, 0), 50, 50,
                new List<IAbilityInstance>());

            return new CombatState(
                new List<IUnit> { caster, victimEast, victimWest },
                new List<IPlayer> { _player, _enemyOwner },
                currentPlayer: _player,
                phase: CombatPhase.Combat);
        }

        private Unit CasterWithQueuedAbility(IAbilityInstance ability, HexDirection facing)
        {
            return new Unit(CasterId, _player, new HexCoordinates(0, 0), 50, 50,
                    new List<IAbilityInstance> { ability },
                    new List<ScheduledAbility> { new ScheduledAbility(ability, 0) })
                .WithFacingDirection(facing);
        }

        [Test]
        public void LineAbility_FiresAlongCasterFacing()
        {
            var ability = LineDamageAbility();
            var caster = CasterWithQueuedAbility(ability, HexDirection.E);
            var state = BuildState(caster);

            var newState = _abilityExecutor.ExecuteAbility(state, caster, caster.AbilityQueue[0]);

            Assert.AreEqual(50 - BaseDamage, newState.GetUnit(VictimEastId).CurrentHP, "east victim hit");
            Assert.AreEqual(50, newState.GetUnit(VictimWestId).CurrentHP, "west victim untouched");
        }

        [Test]
        public void TurningBetweenScheduleAndExecute_RepointsTheVolley()
        {
            var ability = LineDamageAbility();
            // Scheduled while facing east...
            var caster = CasterWithQueuedAbility(ability, HexDirection.E);
            var state = BuildState(caster);

            // ...then the unit turns west (free action) before executing.
            var turned = _actionExecutor.Execute(
                state, new ChangeDirectionAction(_player, CasterId, HexDirection.W));
            var executed = _actionExecutor.Execute(
                turned, new ExecuteAbilityQueueAction(_player, CasterId));

            Assert.AreEqual(50, executed.GetUnit(VictimEastId).CurrentHP, "east victim dodged");
            Assert.AreEqual(50 - BaseDamage, executed.GetUnit(VictimWestId).CurrentHP, "west victim hit");
        }

        [Test]
        public void TwoQueuedLineAbilities_BothFireAlongTheFinalFacing()
        {
            var first = LineDamageAbility();
            var second = new AbilityInstance(new DataDrivenDamageAbility(
                DamageAbilityId + 1, "Test Line 2", 1, AbilityShapeData.ForLine(2), BaseDamage));

            var caster = new Unit(CasterId, _player, new HexCoordinates(0, 0), 50, 50,
                    new List<IAbilityInstance> { first, second },
                    new List<ScheduledAbility>
                    {
                        new ScheduledAbility(first, 0),
                        new ScheduledAbility(second, 1)
                    })
                .WithFacingDirection(HexDirection.W);
            var state = BuildState(caster);

            var executed = _actionExecutor.Execute(
                state, new ExecuteAbilityQueueAction(_player, CasterId));

            Assert.AreEqual(50, executed.GetUnit(VictimEastId).CurrentHP, "east victim untouched");
            Assert.AreEqual(50 - 2 * BaseDamage, executed.GetUnit(VictimWestId).CurrentHP,
                "west victim hit by the whole volley");
        }

        [Test]
        public void RingAbility_HitsAllAround_RegardlessOfFacing()
        {
            var ability = RingDamageAbility();
            var caster = CasterWithQueuedAbility(ability, HexDirection.NE);
            var state = BuildState(caster);

            var newState = _abilityExecutor.ExecuteAbility(state, caster, caster.AbilityQueue[0]);

            Assert.AreEqual(50 - BaseDamage, newState.GetUnit(VictimEastId).CurrentHP, "east victim hit");
            Assert.AreEqual(50 - BaseDamage, newState.GetUnit(VictimWestId).CurrentHP, "west victim hit");
        }

        [Test]
        public void ScheduleWithFacingToSet_TurnsTheUnit()
        {
            var ability = LineDamageAbility();
            var caster = new Unit(CasterId, _player, new HexCoordinates(0, 0), 50, 50,
                new List<IAbilityInstance> { ability });
            var state = BuildState(caster);

            var newState = _actionExecutor.Execute(
                state, new ScheduleAbilityAction(_player, CasterId, DamageAbilityId, HexDirection.SW));

            var updated = newState.GetUnit(CasterId);
            Assert.AreEqual(HexDirection.SW, updated.FacingDirection);
            Assert.AreEqual(1, updated.AbilityQueue.Count);
        }
    }
}
