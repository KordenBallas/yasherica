using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Player;
using Combat.TurnManagement;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// D6 — reveal = orient: when plans are revealed, every armed enemy turns toward its
    /// committed action (a move faces its step, a line ability faces its committed facing),
    /// so reading a unit's orientation is reading what it is about to do.
    /// </summary>
    [TestFixture]
    public class EnemyIntentFacingApplierTests
    {
        private const int EnemyUnitId = 10;
        private const int AbilityId = 100;

        private HexDirectionConfig _config;
        private HumanPlayer _human;
        private AIPlayer _ai;

        private sealed class NoopAI : IAIDecisionMaker
        {
            public IAction DecideAction(ICombatState gameState, IUnit unit) =>
                new EndUnitTurnAction(unit.Owner, unit.Id);
        }

        [SetUp]
        public void SetUp()
        {
            _config = TestHexDirectionConfig.CreateFlatTop();
            _human = new HumanPlayer(1, "Player");
            _ai = new AIPlayer(100, "Enemy", new NoopAI());
        }

        [TearDown]
        public void TearDown()
        {
            TestHexDirectionConfig.Destroy(_config);
        }

        private Unit Enemy(HexCoordinates position, int hp = 30, HexDirection facing = HexDirection.E)
        {
            return new Unit(EnemyUnitId, _ai, position, hp, 30,
                new List<IAbilityInstance>(), facingDirection: facing);
        }

        private CombatState StateWith(params IUnit[] units)
        {
            return new CombatState(
                units.ToList(),
                new List<IPlayer> { _human, _ai },
                _human,
                phase: CombatPhase.Combat);
        }

        private EnemyIntent MoveIntent(Unit enemy, HexCoordinates destination)
        {
            var move = new MoveAction(_ai, enemy.Id, destination);
            return new EnemyIntent(enemy.Id, move, null, enemy.Position, null);
        }

        [Test]
        public void CommittedMove_TurnsTheUnitTowardItsStep()
        {
            var enemy = Enemy(new HexCoordinates(2, 0), facing: HexDirection.E);
            var state = StateWith(enemy);
            var intents = new List<EnemyIntent> { MoveIntent(enemy, new HexCoordinates(1, 0)) };

            var result = EnemyIntentFacingApplier.Apply(state, intents, _config);

            Assert.AreEqual(HexDirection.W, result.GetUnit(EnemyUnitId).FacingDirection);
        }

        [Test]
        public void CommittedMove_MultipleStepsAway_TurnsTowardTheTarget()
        {
            var enemy = Enemy(new HexCoordinates(2, 0), facing: HexDirection.E);
            var state = StateWith(enemy);
            var intents = new List<EnemyIntent> { MoveIntent(enemy, new HexCoordinates(0, 0)) };

            var result = EnemyIntentFacingApplier.Apply(state, intents, _config);

            Assert.AreEqual(HexDirection.W, result.GetUnit(EnemyUnitId).FacingDirection);
        }

        [Test]
        public void CommittedLineAbility_TurnsTheUnitTowardItsCommittedFacing()
        {
            var enemy = Enemy(new HexCoordinates(2, 0), facing: HexDirection.E);
            var state = StateWith(enemy);
            var action = new ScheduleAbilityAction(_ai, enemy.Id, AbilityId, HexDirection.SW);
            var intents = new List<EnemyIntent>
            {
                new EnemyIntent(enemy.Id, action, HexDirection.SW, enemy.Position,
                    new List<HexCoordinates>())
            };

            var result = EnemyIntentFacingApplier.Apply(state, intents, _config);

            Assert.AreEqual(HexDirection.SW, result.GetUnit(EnemyUnitId).FacingDirection);
        }

        [Test]
        public void IntentWithoutDirection_LeavesTheFacingAlone()
        {
            var enemy = Enemy(new HexCoordinates(2, 0), facing: HexDirection.NE);
            var state = StateWith(enemy);
            var action = new EndUnitTurnAction(_ai, enemy.Id);
            var intents = new List<EnemyIntent>
            {
                new EnemyIntent(enemy.Id, action, null, enemy.Position, null)
            };

            var result = EnemyIntentFacingApplier.Apply(state, intents, _config);

            Assert.AreEqual(HexDirection.NE, result.GetUnit(EnemyUnitId).FacingDirection);
        }

        [Test]
        public void DeadUnit_IsSkipped()
        {
            var enemy = Enemy(new HexCoordinates(2, 0), hp: 0, facing: HexDirection.E);
            var state = StateWith(enemy);
            var intents = new List<EnemyIntent> { MoveIntent(enemy, new HexCoordinates(1, 0)) };

            var result = EnemyIntentFacingApplier.Apply(state, intents, _config);

            Assert.AreEqual(HexDirection.E, result.GetUnit(EnemyUnitId).FacingDirection);
        }

        [Test]
        public void MoveToTheOwnCell_LeavesTheFacingAlone()
        {
            var enemy = Enemy(new HexCoordinates(2, 0), facing: HexDirection.NW);
            var state = StateWith(enemy);
            var intents = new List<EnemyIntent> { MoveIntent(enemy, new HexCoordinates(2, 0)) };

            var result = EnemyIntentFacingApplier.Apply(state, intents, _config);

            Assert.AreEqual(HexDirection.NW, result.GetUnit(EnemyUnitId).FacingDirection);
        }
    }
}
