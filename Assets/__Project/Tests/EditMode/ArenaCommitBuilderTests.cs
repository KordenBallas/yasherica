using System.Collections.Generic;
using System.Linq;
using Combat.Arena.Core;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Player;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Proves the lock-time snapshot (R2/whiff honesty): an executed volley flattens into
    /// per-ability committed intents whose cells are frozen from the unit's position + final
    /// facing; a move commits a single step; schedule/pass lock in with an empty commitment;
    /// AI single-action commits mirror the PvE planner's schedule-and-face shape.
    /// </summary>
    [TestFixture]
    public class ArenaCommitBuilderTests
    {
        private const int AbilityIdA = 100;
        private const int AbilityIdB = 101;

        private HexDirectionConfig _config;
        private AbilityShapeCalculator _shapeCalculator;
        private ArenaCommitBuilder _builder;
        private HumanPlayer _player;

        private sealed class NoopAI : IAIDecisionMaker
        {
            public IAction DecideAction(ICombatState gameState, IUnit unit) =>
                new EndUnitTurnAction(unit.Owner, unit.Id);
        }

        [SetUp]
        public void SetUp()
        {
            _config = TestHexDirectionConfig.CreateFlatTop();
            _shapeCalculator = new AbilityShapeCalculator(_config);
            _builder = new ArenaCommitBuilder(_shapeCalculator);
            _player = new HumanPlayer(1, "P1");
        }

        [TearDown]
        public void TearDown()
        {
            TestHexDirectionConfig.Destroy(_config);
        }

        private static IAbilityInstance LineAbility(int id) =>
            new AbilityInstance(new DataDrivenDamageAbility(id, $"Line {id}", 2, AbilityShapeData.ForLine(2), 10));

        private Unit UnitWithQueue(params IAbilityInstance[] queued)
        {
            var abilities = queued.ToList();
            var queue = abilities.Select((a, i) => new ScheduledAbility(a, i)).ToList();
            return new Unit(1, _player, new HexCoordinates(2, 0), 30, 30, abilities, queue,
                facingDirection: HexDirection.W);
        }

        private CombatState StateWith(Unit unit) =>
            new CombatState(new List<IUnit> { unit }, new List<IPlayer> { _player }, _player,
                phase: CombatPhase.Combat);

        [Test]
        public void ExecuteQueue_FlattensIntoPerAbilityCommittedSteps_WithFrozenCells()
        {
            var unit = UnitWithQueue(LineAbility(AbilityIdA), LineAbility(AbilityIdB));
            var state = StateWith(unit);

            var commit = _builder.FromTerminalAction(
                state, unit, new ExecuteAbilityQueueAction(_player, unit.Id));

            Assert.AreEqual(2, commit.Steps.Count, "one step per queued ability");
            Assert.AreEqual(AbilityIdA, commit.Steps[0].AbilityId);
            Assert.AreEqual(AbilityIdB, commit.Steps[1].AbilityId);
            Assert.AreEqual(HexDirection.W, commit.FinalFacing);

            var expectedCells = _shapeCalculator.GetAffectedCells(
                AbilityShapeData.ForLine(2), unit.Position, HexDirection.W, _ => true);
            CollectionAssert.AreEqual(expectedCells, commit.Steps[0].CommittedCells,
                "cells frozen from position + final facing at lock time");
            Assert.AreEqual(unit.Position, commit.Steps[0].CommittedOrigin);
        }

        [Test]
        public void Move_CommitsASingleMoveStep()
        {
            var unit = UnitWithQueue();
            var state = StateWith(unit);
            var move = new MoveAction(_player, unit.Id, new HexCoordinates(3, 0));

            var commit = _builder.FromTerminalAction(state, unit, move);

            Assert.AreEqual(1, commit.Steps.Count);
            Assert.IsTrue(commit.Steps[0].IsMove);
            Assert.AreEqual(new HexCoordinates(3, 0), commit.Steps[0].MoveDestination);
        }

        [Test]
        public void ScheduleAndPass_LockInWithEmptyCommitments()
        {
            var unit = UnitWithQueue(LineAbility(AbilityIdA));
            var state = StateWith(unit);

            var scheduleCommit = _builder.FromTerminalAction(
                state, unit, new ScheduleAbilityAction(_player, unit.Id, AbilityIdA));
            var passCommit = _builder.FromTerminalAction(
                state, unit, new EndUnitTurnAction(_player, unit.Id));

            Assert.IsEmpty(scheduleCommit.Steps, "growing the queue shows nothing to anyone");
            Assert.IsEmpty(passCommit.Steps);
            Assert.AreEqual(HexDirection.W, scheduleCommit.FinalFacing, "aiming still converges");
        }

        [Test]
        public void AiScheduleAndFace_CommitsOneFiringStepAlongTheFacing()
        {
            var ai = new AIPlayer(2, "AI", new NoopAI());
            var ability = LineAbility(AbilityIdA);
            var unit = new Unit(2, ai, new HexCoordinates(2, 0), 30, 30,
                new List<IAbilityInstance> { ability });
            var state = new CombatState(new List<IUnit> { unit }, new List<IPlayer> { ai }, ai,
                phase: CombatPhase.Combat);

            var commit = _builder.FromAiAction(
                state, unit, new ScheduleAbilityAction(ai, unit.Id, AbilityIdA, HexDirection.W));

            Assert.AreEqual(1, commit.Steps.Count, "AI single action = one firing step (PvE enemy shape)");
            Assert.AreEqual(HexDirection.W, commit.FinalFacing, "FacingToSet becomes the final facing");
            Assert.AreEqual(HexDirection.W, commit.Steps[0].CommittedFacing);
            Assert.IsNotEmpty(commit.Steps[0].CommittedCells.ToList());
        }
    }
}
