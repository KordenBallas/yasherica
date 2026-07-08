using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Execution;
using Combat.Player;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Proves the ghost's honesty: the predicted outcome equals the actual execution result
    /// (HP deltas and landing cells) on an unchanged board, and prediction never mutates state.
    /// </summary>
    [TestFixture]
    public class AbilityOutcomeCalculatorTests
    {
        private const int CasterId = 1;
        private const int VictimId = 2;
        private const int SecondVictimId = 3;
        private const int PushAbilityId = 200;
        private const int BaseDamage = 20;

        private HexDirectionConfig _config;
        private AbilityOutcomeCalculator _calculator;
        private AbilityExecutor _executor;
        private AbilityShapeCalculator _shapeCalculator;
        private HumanPlayer _player;
        private HumanPlayer _enemyOwner;

        [SetUp]
        public void SetUp()
        {
            _config = TestHexDirectionConfig.CreateFlatTop();
            _shapeCalculator = new AbilityShapeCalculator(_config);
            var damageSystem = new DamageSystem();
            _calculator = new AbilityOutcomeCalculator(_shapeCalculator, damageSystem, _config);
            _executor = new AbilityExecutor(damageSystem, null, _shapeCalculator, _config);
            _player = new HumanPlayer(1, "Player");
            _enemyOwner = new HumanPlayer(2, "Enemies");
        }

        [TearDown]
        public void TearDown()
        {
            TestHexDirectionConfig.Destroy(_config);
        }

        private static IAbilityInstance PushAbility(int pushDistance = 2)
        {
            return new AbilityInstance(new DataDrivenDamageAbility(
                PushAbilityId, "Test Push", 1, AbilityShapeData.ForLine(2), BaseDamage, pushDistance));
        }

        private static IAbilityInstance RingAbility()
        {
            return new AbilityInstance(new DataDrivenDamageAbility(
                PushAbilityId + 1, "Test Ring", 1, AbilityShapeData.ForRing(1), BaseDamage));
        }

        private Unit Caster(IAbilityInstance ability, HexDirection facing = HexDirection.E)
        {
            return new Unit(CasterId, _player, new HexCoordinates(0, 0), 50, 50,
                    new List<IAbilityInstance> { ability },
                    new List<ScheduledAbility> { new ScheduledAbility(ability, 0) })
                .WithFacingDirection(facing);
        }

        private Unit Victim(int id, HexCoordinates position)
        {
            return new Unit(id, _enemyOwner, position, 50, 50, new List<IAbilityInstance>());
        }

        private CombatState StateWith(params IUnit[] units)
        {
            return new CombatState(units.ToList(),
                new List<IPlayer> { _player, _enemyOwner },
                _player, phase: CombatPhase.Combat);
        }

        [Test]
        public void PredictedOutcome_MatchesActualExecution_DamageAndPush()
        {
            var ability = PushAbility();
            var caster = Caster(ability);
            var near = Victim(VictimId, new HexCoordinates(1, 0));
            var far = Victim(SecondVictimId, new HexCoordinates(2, 0));
            var state = StateWith(caster, near, far);

            var outcome = _calculator.ComputeForFacing(state, caster, ability.Ability, HexDirection.E);
            var executed = _executor.ExecuteAbility(state, caster, caster.AbilityQueue[0]);

            foreach (var predicted in outcome.Units)
            {
                var actual = executed.GetUnit(predicted.UnitId);
                var before = state.GetUnit(predicted.UnitId);
                Assert.AreEqual(before.CurrentHP - actual.CurrentHP, predicted.Damage,
                    $"HP delta for unit {predicted.UnitId}");
                Assert.AreEqual(actual.Position, predicted.To,
                    $"landing cell for unit {predicted.UnitId}");
            }
            Assert.AreEqual(2, outcome.Units.Count, "both struck units predicted");
        }

        [Test]
        public void Prediction_DoesNotMutateTheState()
        {
            var ability = PushAbility();
            var caster = Caster(ability);
            var victim = Victim(VictimId, new HexCoordinates(1, 0));
            var state = StateWith(caster, victim);

            _calculator.ComputeForFacing(state, caster, ability.Ability, HexDirection.E);

            Assert.AreEqual(50, state.GetUnit(VictimId).CurrentHP);
            Assert.AreEqual(new HexCoordinates(1, 0), state.GetUnit(VictimId).Position);
        }

        [Test]
        public void RingOutcome_IgnoresFacing()
        {
            var ability = RingAbility();
            var caster = Caster(ability, HexDirection.NW);
            var east = Victim(VictimId, new HexCoordinates(1, 0));
            var west = Victim(SecondVictimId, new HexCoordinates(-1, 0));
            var state = StateWith(caster, east, west);

            var outcome = _calculator.ComputeForFacing(state, caster, ability.Ability, HexDirection.NW);

            var struck = outcome.Units.Select(u => u.UnitId).ToList();
            CollectionAssert.Contains(struck, VictimId);
            CollectionAssert.Contains(struck, SecondVictimId);
            Assert.IsTrue(outcome.Units.All(u => !u.IsDisplaced), "ring has no line direction to push along");
        }

        [Test]
        public void ComputeCommitted_UsesTheSnapshottedCells()
        {
            var ability = PushAbility();
            var enemyAiOwner = new AIPlayer(100, "Enemy", null);
            var enemy = new Unit(20, enemyAiOwner, new HexCoordinates(2, 0), 30, 30,
                new List<IAbilityInstance> { ability }).WithFacingDirection(HexDirection.W);
            var hero = Victim(VictimId, new HexCoordinates(1, 0));
            var state = StateWith(enemy, hero);

            var committedCells = _shapeCalculator.GetAffectedCells(
                ability.Ability.Shape, enemy.Position, HexDirection.W, state.IsPositionValid);
            var intent = new EnemyIntent(
                20,
                new ScheduleAbilityAction(enemyAiOwner, 20, PushAbilityId, HexDirection.W),
                HexDirection.W,
                enemy.Position,
                committedCells);

            var outcome = _calculator.ComputeCommitted(state, intent);

            var predicted = outcome.Units.Single(u => u.UnitId == VictimId);
            Assert.AreEqual(BaseDamage, predicted.Damage);
            Assert.IsTrue(predicted.IsDisplaced, "hero predicted pushed west");
            Assert.AreEqual(new HexCoordinates(-1, 0), predicted.To, "pushed 2 cells west of (1,0)");
        }

        [Test]
        public void ComputeForFacing_OriginEqualToCasterPosition_MatchesDefaultOverload()
        {
            var ability = PushAbility();
            var caster = Caster(ability);
            var near = Victim(VictimId, new HexCoordinates(1, 0));
            var far = Victim(SecondVictimId, new HexCoordinates(2, 0));
            var state = StateWith(caster, near, far);

            var fromCurrent = _calculator.ComputeForFacing(state, caster, ability.Ability, HexDirection.E);
            var fromOrigin = _calculator.ComputeForFacing(
                state, caster, ability.Ability, HexDirection.E, caster.Position);

            CollectionAssert.AreEqual(
                fromCurrent.AffectedCells.ToList(), fromOrigin.AffectedCells.ToList());
            CollectionAssert.AreEqual(
                fromCurrent.Units.Select(u => (u.UnitId, u.Damage, u.To)).ToList(),
                fromOrigin.Units.Select(u => (u.UnitId, u.Damage, u.To)).ToList());
        }

        [Test]
        public void ComputeForFacing_HypotheticalOrigin_ComputesCellsFromThatOrigin()
        {
            var ability = PushAbility();
            var caster = Caster(ability);
            var victim = Victim(VictimId, new HexCoordinates(3, 0));
            var state = StateWith(caster, victim);

            // From (0,0) the length-2 east line covers (1,0),(2,0) — misses the victim.
            var fromCurrent = _calculator.ComputeForFacing(state, caster, ability.Ability, HexDirection.E);
            // From the hypothetical origin (2,0) it covers (3,0),(4,0) — hits the victim.
            var fromOrigin = _calculator.ComputeForFacing(
                state, caster, ability.Ability, HexDirection.E, new HexCoordinates(2, 0));

            Assert.IsEmpty(fromCurrent.Units, "no unit in the line from the current position");
            Assert.AreEqual(VictimId, fromOrigin.Units.Single().UnitId);
            CollectionAssert.Contains(fromOrigin.AffectedCells.ToList(), new HexCoordinates(3, 0));
        }

        [Test]
        public void ComputeCommitted_NonAbilityIntent_YieldsEmptyOutcome()
        {
            var enemyAiOwner = new AIPlayer(100, "Enemy", null);
            var enemy = new Unit(20, enemyAiOwner, new HexCoordinates(2, 0), 30, 30,
                new List<IAbilityInstance>());
            var state = StateWith(enemy);
            var intent = new EnemyIntent(
                20, new MoveAction(enemyAiOwner, 20, new HexCoordinates(1, 0)), null, enemy.Position, null);

            var outcome = _calculator.ComputeCommitted(state, intent);

            Assert.IsEmpty(outcome.Units);
            Assert.IsEmpty(outcome.AffectedCells);
        }
    }
}
