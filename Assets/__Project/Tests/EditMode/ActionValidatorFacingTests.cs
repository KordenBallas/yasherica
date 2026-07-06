using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Execution;
using Combat.Player;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class ActionValidatorFacingTests
    {
        private HumanPlayer _player;
        private ActionValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _player = new HumanPlayer(1, "Player");
            _validator = new ActionValidator(
                new CombatConfig(1f, HexOrientation.Flat, maxAbilityQueueSize: 3));
        }

        private CombatState StateWith(Unit unit, RoundPhase roundPhase = RoundPhase.PlayerAct)
        {
            return new CombatState(
                new List<IUnit> { unit },
                new List<IPlayer> { _player },
                currentPlayer: _player,
                phase: CombatPhase.Combat,
                roundPhase: roundPhase);
        }

        private Unit MakeUnit(bool hasActed, IReadOnlyList<IStatusEffect> effects = null)
        {
            return new Unit(
                id: 10,
                owner: _player,
                position: new HexCoordinates(0, 0),
                currentHP: 50,
                maxHP: 50,
                abilities: new List<IAbilityInstance>(),
                statusEffects: effects,
                hasActedThisTurn: hasActed);
        }

        [Test]
        public void ChangeDirection_AllowedAfterUnitHasActed()
        {
            var state = StateWith(MakeUnit(hasActed: true));
            var action = new ChangeDirectionAction(_player, 10, HexDirection.W);

            var result = _validator.ValidateDetailed(state, action);

            Assert.IsTrue(result.IsValid, result.FailureReason);
        }

        [Test]
        public void TurnEndingAction_StillBlockedAfterUnitHasActed()
        {
            var state = StateWith(MakeUnit(hasActed: true));
            var action = new EndUnitTurnAction(_player, 10);

            var result = _validator.ValidateDetailed(state, action);

            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void ChangeDirection_BlockedWhileStunned()
        {
            var stunned = MakeUnit(
                hasActed: false,
                effects: new List<IStatusEffect> { new StunEffect(1) });
            var state = StateWith(stunned);
            var action = new ChangeDirectionAction(_player, 10, HexDirection.W);

            var result = _validator.ValidateDetailed(state, action);

            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void ChangeDirection_AllowedBeforeActing()
        {
            var state = StateWith(MakeUnit(hasActed: false));
            var action = new ChangeDirectionAction(_player, 10, HexDirection.NE);

            var result = _validator.ValidateDetailed(state, action);

            Assert.IsTrue(result.IsValid, result.FailureReason);
        }

        [Test]
        public void Move_OntoACorpseCell_IsValid()
        {
            // D5: a dead unit frees its cell the moment it dies — the corpse is not a blocker.
            var mover = MakeUnit(hasActed: false);
            var corpse = new Unit(
                id: 11,
                owner: _player,
                position: new HexCoordinates(1, 0),
                currentHP: 0,
                maxHP: 50,
                abilities: new List<IAbilityInstance>());
            var state = new CombatState(
                new List<IUnit> { mover, corpse },
                new List<IPlayer> { _player },
                currentPlayer: _player,
                phase: CombatPhase.Combat,
                roundPhase: RoundPhase.PlayerAct);
            var action = new MoveAction(_player, 10, new HexCoordinates(1, 0));

            var result = _validator.ValidateDetailed(state, action);

            Assert.IsTrue(result.IsValid, result.FailureReason);
        }

        [Test]
        public void AnyPlayerAction_BlockedOutsideTheActPhase()
        {
            var state = StateWith(MakeUnit(hasActed: false), RoundPhase.EnemyResolve);
            var action = new ChangeDirectionAction(_player, 10, HexDirection.W);

            var result = _validator.ValidateDetailed(state, action);

            Assert.IsFalse(result.IsValid);
        }
    }
}
