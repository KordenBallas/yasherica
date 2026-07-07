using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Core.StatusEffects;
using Combat.Execution;
using Combat.Player;
using Core.Logging;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// The three data-authored control kinds gate exactly what the brief says (FR2): Stun loses
    /// the turn (ActionState + validator + committed-intent skip), Root pins but still acts,
    /// Slow shrinks the move budget — one shared MovementRange source for the validator, the
    /// rules, and the committed-move fizzle.
    /// </summary>
    [TestFixture]
    public class ControlGatingTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        private const int UnitId = 10;
        private const int AbilityId = 100;

        private HumanPlayer _player;
        private ActionValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _player = new HumanPlayer(1, "Player");
            _validator = new ActionValidator(
                new CombatConfig(1f, HexOrientation.Flat, maxAbilityQueueSize: 3));
        }

        private static IStatusEffect Control(ControlKind kind, int duration = 2, int penalty = 1,
            int stackCount = 1)
        {
            return new DataDrivenControlEffect(
                60, kind.ToString(), duration, kind, penalty,
                StatusEffectTriggerType.TurnEnd, stackCount, StackRule.Refresh);
        }

        private Unit MakeUnit(params IStatusEffect[] effects)
        {
            var ability = new AbilityInstance(new DataDrivenDamageAbility(
                AbilityId, "Test Line", 1, AbilityShapeData.ForLine(1), 10));
            return new Unit(UnitId, _player, new HexCoordinates(0, 0), 50, 50,
                new List<IAbilityInstance> { ability },
                statusEffects: effects.Length > 0 ? effects : null);
        }

        private CombatState StateWith(Unit unit)
        {
            return new CombatState(
                new List<IUnit> { unit },
                new List<IPlayer> { _player },
                currentPlayer: _player,
                phase: CombatPhase.Combat,
                roundPhase: RoundPhase.PlayerAct);
        }

        // ---- Stun ----

        [Test]
        public void DataDrivenStun_ReadsAsStunned()
        {
            Assert.AreEqual(UnitActionState.Stunned, MakeUnit(Control(ControlKind.Stun)).ActionState);
        }

        [Test]
        public void Stun_BlocksEveryAction()
        {
            var state = StateWith(MakeUnit(Control(ControlKind.Stun)));

            var move = _validator.ValidateDetailed(
                state, new MoveAction(_player, UnitId, new HexCoordinates(1, 0)));
            var schedule = _validator.ValidateDetailed(
                state, new ScheduleAbilityAction(_player, UnitId, AbilityId, HexDirection.E));

            Assert.IsFalse(move.IsValid);
            Assert.IsFalse(schedule.IsValid);
        }

        [Test]
        public void Stun_SkipsACommittedIntentAtResolve()
        {
            var stunned = MakeUnit(Control(ControlKind.Stun));
            var state = StateWith(stunned);
            var resolver = new EnemyIntentResolver(NullAbilityExecutor(), new FakeLogger());

            var intent = new EnemyIntent(
                UnitId, new MoveAction(_player, UnitId, new HexCoordinates(1, 0)),
                null, new HexCoordinates(0, 0), new List<HexCoordinates>());
            var resolved = resolver.Resolve(state, intent);

            Assert.AreEqual(new HexCoordinates(0, 0), resolved.GetUnit(UnitId).Position,
                "a stunned committer's intent is skipped whole");
        }

        // ---- Root ----

        [Test]
        public void Root_ZeroesTheMoveBudget_ButTheUnitStillActs()
        {
            var rooted = MakeUnit(Control(ControlKind.Root));

            Assert.AreEqual(0, MovementRange.EffectiveFor(rooted));
            Assert.IsFalse(rooted.CanMove(), "rooted cannot move");
            Assert.AreEqual(UnitActionState.Ready, rooted.ActionState, "rooted is not stunned");
            Assert.IsTrue(rooted.CanScheduleAbility(), "rooted may still act");
        }

        [Test]
        public void Root_RejectsAnyMove_AllowsScheduling()
        {
            var state = StateWith(MakeUnit(Control(ControlKind.Root)));

            var move = _validator.ValidateDetailed(
                state, new MoveAction(_player, UnitId, new HexCoordinates(1, 0)));
            var schedule = _validator.ValidateDetailed(
                state, new ScheduleAbilityAction(_player, UnitId, AbilityId, HexDirection.E));

            Assert.IsFalse(move.IsValid, "rooted unit cannot move even one cell");
            Assert.IsTrue(schedule.IsValid, schedule.FailureReason);
        }

        [Test]
        public void Root_LandedAfterTheCommit_FizzlesTheCommittedMove()
        {
            // The move was committed while free; the root landed during the player's Act.
            var rooted = MakeUnit(Control(ControlKind.Root));
            var state = StateWith(rooted);
            var resolver = new EnemyIntentResolver(NullAbilityExecutor(), new FakeLogger());

            var intent = new EnemyIntent(
                UnitId, new MoveAction(_player, UnitId, new HexCoordinates(1, 0)),
                null, new HexCoordinates(0, 0), new List<HexCoordinates>());
            var resolved = resolver.Resolve(state, intent);

            Assert.AreEqual(new HexCoordinates(0, 0), resolved.GetUnit(UnitId).Position,
                "the committed move fizzles under the new budget");
        }

        // ---- Slow ----

        [Test]
        public void Slow_ShrinksTheMoveBudgetByItsPenaltyPerStack()
        {
            Assert.AreEqual(2, MovementRange.EffectiveFor(MakeUnit(Control(ControlKind.Slow))));
            Assert.AreEqual(1, MovementRange.EffectiveFor(
                MakeUnit(Control(ControlKind.Slow, stackCount: 2))));
            Assert.AreEqual(0, MovementRange.EffectiveFor(
                MakeUnit(Control(ControlKind.Slow, penalty: 5))), "budget floors at zero");
        }

        [Test]
        public void Slow_RejectsAMoveBeyondTheShrunkRange_AcceptsWithin()
        {
            var state = StateWith(MakeUnit(Control(ControlKind.Slow)));

            var farMove = _validator.ValidateDetailed(
                state, new MoveAction(_player, UnitId, new HexCoordinates(3, 0)));
            var nearMove = _validator.ValidateDetailed(
                state, new MoveAction(_player, UnitId, new HexCoordinates(2, 0)));

            Assert.IsFalse(farMove.IsValid, "3 cells exceeds the slowed budget of 2");
            Assert.IsTrue(nearMove.IsValid, nearMove.FailureReason);
        }

        [Test]
        public void MovementRules_UseTheSameGatedBudget()
        {
            var rules = new Combat.Rules.MovementRules();
            var slowed = MakeUnit(Control(ControlKind.Slow));
            var state = StateWith(slowed);

            Assert.IsFalse(rules.CanMoveTo(slowed, new HexCoordinates(3, 0), state));
            Assert.IsTrue(rules.CanMoveTo(slowed, new HexCoordinates(2, 0), state));
        }

        private static IAbilityExecutor NullAbilityExecutor()
        {
            // The resolver only touches the executor for ability intents; these tests resolve moves.
            return null;
        }
    }
}
