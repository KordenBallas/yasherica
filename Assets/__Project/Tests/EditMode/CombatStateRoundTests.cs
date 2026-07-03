using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Player;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class CombatStateRoundTests
    {
        private static CombatState MakeState()
        {
            var player = new HumanPlayer(1, "Player");
            var unit = new Unit(1, player, new HexCoordinates(0, 0), 50, 50,
                new List<IAbilityInstance>());
            return new CombatState(
                new List<IUnit> { unit },
                new List<IPlayer> { player },
                player,
                turnNumber: 3,
                phase: CombatPhase.Combat);
        }

        private static EnemyIntent SomeIntent()
        {
            var owner = new HumanPlayer(2, "Enemy");
            return new EnemyIntent(
                10,
                new EndUnitTurnAction(owner, 10),
                HexDirection.W,
                new HexCoordinates(2, 0),
                new List<HexCoordinates> { new HexCoordinates(1, 0) });
        }

        [Test]
        public void Defaults_AreEnemyPlanAndNoIntents()
        {
            var state = MakeState();

            Assert.AreEqual(RoundPhase.EnemyPlan, state.RoundPhase);
            Assert.IsEmpty(state.EnemyIntents);
        }

        [Test]
        public void WithRoundPhase_PreservesEverythingElse()
        {
            var state = MakeState().WithEnemyIntents(new List<EnemyIntent> { SomeIntent() });

            var next = state.WithRoundPhase(RoundPhase.EnemyResolve);

            Assert.AreEqual(RoundPhase.EnemyResolve, next.RoundPhase);
            Assert.AreEqual(state.TurnNumber, next.TurnNumber);
            Assert.AreEqual(state.Phase, next.Phase);
            Assert.AreEqual(state.Units.Count, next.Units.Count);
            Assert.AreEqual(1, next.EnemyIntents.Count, "intents preserved");
        }

        [Test]
        public void WithEnemyIntents_PreservesRoundPhase()
        {
            var state = MakeState().WithRoundPhase(RoundPhase.PlayerAct);

            var next = state.WithEnemyIntents(new List<EnemyIntent> { SomeIntent() });

            Assert.AreEqual(RoundPhase.PlayerAct, next.RoundPhase);
            Assert.AreEqual(1, next.EnemyIntents.Count);
        }

        [Test]
        public void OtherWithMethods_PreserveRoundPhaseAndIntents()
        {
            var state = MakeState()
                .WithRoundPhase(RoundPhase.EnemyResolve)
                .WithEnemyIntents(new List<EnemyIntent> { SomeIntent() });

            var afterUnitUpdate = state.WithUpdatedUnit(state.Units[0]);
            var afterNextTurn = state.WithNextTurn();
            var afterPhase = state.WithPhase(CombatPhase.Victory);

            Assert.AreEqual(RoundPhase.EnemyResolve, afterUnitUpdate.RoundPhase);
            Assert.AreEqual(1, afterUnitUpdate.EnemyIntents.Count);
            Assert.AreEqual(RoundPhase.EnemyResolve, afterNextTurn.RoundPhase);
            Assert.AreEqual(1, afterNextTurn.EnemyIntents.Count);
            Assert.AreEqual(RoundPhase.EnemyResolve, afterPhase.RoundPhase);
            Assert.AreEqual(1, afterPhase.EnemyIntents.Count);
        }
    }
}
