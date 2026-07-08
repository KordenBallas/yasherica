using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Player;
using Combat.TurnManagement;
using Core.Logging;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Proves the Plan phase: all enemies decide up front in UnitId order, committed cells
    /// are snapshotted from the plan-time origin + facing, and the same seed + same state
    /// yields identical plans (the intent-phase determinism guarantee).
    /// </summary>
    [TestFixture]
    public class EnemyIntentPlannerTests
    {
        private sealed class FakeLogger : IGameLogger
        {
            public void Info(LogCategory category, string message) { }
            public void Warning(LogCategory category, string message) { }
            public void Error(LogCategory category, string message) { }
        }

        /// <summary>
        /// Deterministic decision maker that always commits the unit's first ability west.
        /// </summary>
        private sealed class FixedAbilityAI : IAIDecisionMaker
        {
            public List<int> DecidedUnitIds { get; } = new List<int>();

            public IAction DecideAction(ICombatState gameState, IUnit unit)
            {
                DecidedUnitIds.Add(unit.Id);
                return new ScheduleAbilityAction(
                    unit.Owner, unit.Id, unit.Abilities[0].Ability.Id, HexDirection.W);
            }
        }

        private const int AbilityId = 100;

        private HexDirectionConfig _config;
        private EnemyIntentPlanner _planner;
        private HumanPlayer _human;

        [SetUp]
        public void SetUp()
        {
            _config = TestHexDirectionConfig.CreateFlatTop();
            _planner = new EnemyIntentPlanner(new AbilityShapeCalculator(_config), new FakeLogger());
            _human = new HumanPlayer(1, "Player");
        }

        [TearDown]
        public void TearDown()
        {
            TestHexDirectionConfig.Destroy(_config);
        }

        private static IAbilityInstance LineAbility()
        {
            return new AbilityInstance(new DataDrivenDamageAbility(
                AbilityId, "Test Line", 1, AbilityShapeData.ForLine(2), 10));
        }

        private Unit EnemyUnit(int id, AIPlayer owner, HexCoordinates position)
        {
            return new Unit(id, owner, position, 30, 30,
                new List<IAbilityInstance> { LineAbility() });
        }

        private CombatState StateWith(params IUnit[] units)
        {
            var players = new List<IPlayer> { _human };
            players.AddRange(units.Select(u => u.Owner).Distinct());
            return new CombatState(units.ToList(), players, _human, phase: CombatPhase.Combat);
        }

        [Test]
        public void Plan_CommitsEveryEnemyUpFront_InUnitIdOrder()
        {
            var ai = new FixedAbilityAI();
            var enemyOwner = new AIPlayer(100, "Enemies", ai);
            var state = StateWith(
                EnemyUnit(30, enemyOwner, new HexCoordinates(3, 0)),
                EnemyUnit(10, enemyOwner, new HexCoordinates(5, 0)),
                EnemyUnit(20, enemyOwner, new HexCoordinates(4, 0)));

            var intents = _planner.Plan(state);

            Assert.AreEqual(3, intents.Count, "every enemy planned");
            CollectionAssert.AreEqual(new[] { 10, 20, 30 }, ai.DecidedUnitIds, "UnitId order");
            CollectionAssert.AreEqual(new[] { 10, 20, 30 }, intents.Select(i => i.UnitId).ToList());
        }

        [Test]
        public void Plan_SnapshotsCommittedCells_FromPlanTimeOriginAndFacing()
        {
            var enemyOwner = new AIPlayer(100, "Enemy", new FixedAbilityAI());
            var state = StateWith(EnemyUnit(10, enemyOwner, new HexCoordinates(2, 0)));

            var intent = _planner.Plan(state)[0];

            Assert.IsTrue(intent.IsAbility);
            Assert.AreEqual(HexDirection.W, intent.CommittedFacing);
            Assert.AreEqual(new HexCoordinates(2, 0), intent.CommittedOrigin);
            // Line length 2 west of (2,0): (1,0), (0,0)
            CollectionAssert.AreEqual(
                new[] { new HexCoordinates(1, 0), new HexCoordinates(0, 0) },
                intent.CommittedCells.ToList());
        }

        [Test]
        public void Plan_SkipsDeadEnemies_AndIgnoresHumanUnits()
        {
            var ai = new FixedAbilityAI();
            var enemyOwner = new AIPlayer(100, "Enemies", ai);
            var deadEnemy = new Unit(10, enemyOwner, new HexCoordinates(5, 0), 0, 30,
                new List<IAbilityInstance> { LineAbility() });
            var humanUnit = new Unit(1, _human, new HexCoordinates(0, 0), 50, 50,
                new List<IAbilityInstance>());
            var liveEnemy = EnemyUnit(20, enemyOwner, new HexCoordinates(4, 0));
            var state = StateWith(deadEnemy, humanUnit, liveEnemy);

            var intents = _planner.Plan(state);

            Assert.AreEqual(1, intents.Count);
            Assert.AreEqual(20, intents[0].UnitId);
        }

        [Test]
        public void Plan_SameSeededAI_SameState_YieldsIdenticalIntents()
        {
            EnemyIntent PlanOnce()
            {
                var seededAi = new Combat.Player.AI.SimulationTacticalAI(
                    Combat.Player.AI.AITuning.Compose(
                        Combat.Player.AI.AIBehaviorProfile.Default,
                        Combat.Player.AI.AIDifficultySettings.Neutral),
                    new Combat.Execution.AbilityOutcomeCalculator(
                        new AbilityShapeCalculator(_config), new Combat.Execution.DamageSystem(), _config),
                    new Combat.Player.AI.TeamHostilityPolicy(),
                    seed: 1234);
                var owner = new AIPlayer(100, "Enemy", seededAi);
                var state = StateWith(
                    EnemyUnit(10, owner, new HexCoordinates(2, 0)),
                    new Unit(1, _human, new HexCoordinates(0, 0), 50, 50, new List<IAbilityInstance>()));
                return _planner.Plan(state)[0];
            }

            var first = PlanOnce();
            var second = PlanOnce();

            Assert.AreEqual(first.Action.Type, second.Action.Type);
            Assert.AreEqual(first.CommittedFacing, second.CommittedFacing);
            CollectionAssert.AreEqual(first.CommittedCells.ToList(), second.CommittedCells.ToList());
        }
    }
}
