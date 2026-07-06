using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Config;
using Combat.Controller;
using Combat.Core;
using Combat.Execution;
using Combat.Player;
using Combat.TurnManagement;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Proves the D3 enemy intent board telegraph models: every live enemy holding a committed intent is
    /// "armed" (→ ready pose + restless icons); a committed move carries its from/to cells (→ arrow); the
    /// player and dead enemies are excluded; and the telegraph clears when combat ends.
    /// </summary>
    [TestFixture]
    public class EnemyIntentTelegraphPresenterTests
    {
        private sealed class FakeView : IEnemyIntentTelegraphView
        {
            public IReadOnlyList<EnemyIntentTelegraphModel> Telegraphs = new List<EnemyIntentTelegraphModel>();
            public bool Cleared;

            public void SetTelegraphs(IReadOnlyList<EnemyIntentTelegraphModel> telegraphs)
            {
                Telegraphs = telegraphs;
                Cleared = false;
            }

            public void Clear() => Cleared = true;
        }

        private sealed class FakeController : ICombatController
        {
            public ICombatState CombatState { get; set; }
            public ITurnManager TurnManager => null;
            public IBattlefield Battlefield => null;
            public CombatInitiator OpeningInitiator => CombatInitiator.Enemy;

            public event System.Action<ICombatState> OnStateChanged;
            public event System.Action<IPlayer> OnTurnStarted { add { } remove { } }
            public event System.Action<RoundPhase> OnRoundPhaseChanged { add { } remove { } }
            public event System.Action<IReadOnlyList<EnemyIntent>> OnEnemyPlansRevealed;
            public event System.Action<IPlayer, CombatPhase> OnGameEnded;

            public void Initialize(ICombatState initialState, IReadOnlyList<IPlayer> players,
                CombatInitiator openingInitiator = CombatInitiator.Enemy) { }
            public void AddUnit(IUnit unit) { }
            public void BeginRounds() { }
            public bool ResolveNextEnemyIntent() => false;
            public ActionResult ProcessAction(IAction action) => null;
            public void Update() { }
            public void InitializeBattlefield(PlatformHexSurface surface, Vector3 center) { }
            public void CleanupBattlefield() { }

            public void RaiseStateChanged() => OnStateChanged?.Invoke(CombatState);
            public void RaisePlansRevealed() => OnEnemyPlansRevealed?.Invoke(CombatState.EnemyIntents);
            public void RaiseGameEnded() => OnGameEnded?.Invoke(null, CombatPhase.Victory);
        }

        private const int HeroId = 1;
        private const int EnemyId = 10;

        private HumanPlayer _human;
        private AIPlayer _enemyOwner;

        private sealed class NoopAI : IAIDecisionMaker
        {
            public IAction DecideAction(ICombatState gameState, IUnit unit) =>
                new EndUnitTurnAction(unit.Owner, unit.Id);
        }

        [SetUp]
        public void SetUp()
        {
            _human = new HumanPlayer(1, "Player");
            _enemyOwner = new AIPlayer(100, "Bandit", new NoopAI());
        }

        private Unit Hero() =>
            new Unit(HeroId, _human, new HexCoordinates(0, 0), 50, 50, new List<IAbilityInstance>());

        private Unit Enemy(int hp = 30) =>
            new Unit(EnemyId, _enemyOwner, new HexCoordinates(3, 0), hp, 30, new List<IAbilityInstance>());

        private CombatState State(IReadOnlyList<EnemyIntent> intents, params IUnit[] units) =>
            new CombatState(units.ToList(), new List<IPlayer> { _human, _enemyOwner }, _human,
                phase: CombatPhase.Combat, enemyIntents: intents);

        private EnemyIntent AbilityIntent() =>
            new EnemyIntent(EnemyId,
                new ScheduleAbilityAction(_enemyOwner, EnemyId, 200, HexDirection.W),
                HexDirection.W, new HexCoordinates(3, 0), new List<HexCoordinates>());

        private EnemyIntent MoveIntent(HexCoordinates from, HexCoordinates to) =>
            new EnemyIntent(EnemyId, new MoveAction(_enemyOwner, EnemyId, to), null, from, null);

        [Test]
        public void AbilityIntent_IsArmed_NoMove()
        {
            var state = State(new List<EnemyIntent> { AbilityIntent() }, Hero(), Enemy());

            var models = EnemyIntentTelegraphPresenter.BuildTelegraphs(state);

            Assert.AreEqual(1, models.Count);
            Assert.AreEqual(EnemyId, models[0].UnitId);
            Assert.IsTrue(models[0].IsArmed);
            Assert.IsFalse(models[0].HasMove);
        }

        [Test]
        public void MoveIntent_IsArmed_WithFromToCells()
        {
            var from = new HexCoordinates(3, 0);
            var to = new HexCoordinates(2, 0);
            var state = State(new List<EnemyIntent> { MoveIntent(from, to) }, Hero(), Enemy());

            var models = EnemyIntentTelegraphPresenter.BuildTelegraphs(state);

            Assert.AreEqual(1, models.Count);
            Assert.IsTrue(models[0].IsArmed);
            Assert.IsTrue(models[0].HasMove);
            Assert.AreEqual(from, models[0].MoveFrom);
            Assert.AreEqual(to, models[0].MoveTo);
        }

        [Test]
        public void PlayerUnit_IsNotTelegraphed()
        {
            // Only enemy intents are telegraphed; a hero with no committed intent produces nothing.
            var state = State(new List<EnemyIntent>(), Hero());

            var models = EnemyIntentTelegraphPresenter.BuildTelegraphs(state);

            Assert.IsEmpty(models);
        }

        [Test]
        public void ResolvePhase_ClearsTheTelegraph()
        {
            // During EnemyResolve the enemies act and move, so the arrow + wind-up pose clear (else the
            // pose would pin the enemy's transform and fight its movement).
            var state = new CombatState(
                new List<IUnit> { Hero(), Enemy() },
                new List<IPlayer> { _human, _enemyOwner },
                _human, phase: CombatPhase.Combat, roundPhase: RoundPhase.EnemyResolve,
                enemyIntents: new List<EnemyIntent> { AbilityIntent() });

            var models = EnemyIntentTelegraphPresenter.BuildTelegraphs(state);

            Assert.IsEmpty(models, "no telegraph while enemies are resolving/moving");
        }

        [Test]
        public void DeadEnemy_IsDropped()
        {
            var state = State(new List<EnemyIntent> { AbilityIntent() }, Hero(), Enemy(hp: 0));

            var models = EnemyIntentTelegraphPresenter.BuildTelegraphs(state);

            Assert.IsEmpty(models, "a dead enemy carries no readiness telegraph");
        }

        [Test]
        public void Presenter_RebuildsOnStateChange_AndClearsOnGameEnd()
        {
            var view = new FakeView();
            var controller = new FakeController
            {
                CombatState = State(new List<EnemyIntent> { AbilityIntent() }, Hero(), Enemy())
            };
            using (new EnemyIntentTelegraphPresenter(controller, view))
            {
                controller.RaiseStateChanged();
                Assert.AreEqual(1, view.Telegraphs.Count);
                Assert.IsFalse(view.Cleared);

                controller.RaiseGameEnded();
                Assert.IsTrue(view.Cleared, "the telegraph clears when combat ends");
            }
        }
    }
}
