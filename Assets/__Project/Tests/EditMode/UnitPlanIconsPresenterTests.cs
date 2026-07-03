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
    /// Proves the plan-icons models: player rows mirror the queue order, enemy rows show the
    /// revealed committed intent (ability icon or move glyph), dead units show nothing.
    /// </summary>
    [TestFixture]
    public class UnitPlanIconsPresenterTests
    {
        private sealed class FakeView : IUnitPlanIconsView
        {
            public readonly Dictionary<int, IReadOnlyList<PlanIconModel>> Icons =
                new Dictionary<int, IReadOnlyList<PlanIconModel>>();
            public bool Cleared;

            public void SetIcons(int unitId, IReadOnlyList<PlanIconModel> icons) => Icons[unitId] = icons;
            public void ClearAll() => Cleared = true;
        }

        private sealed class FakeController : ICombatController
        {
            public ICombatState CombatState { get; set; }
            public ITurnManager TurnManager => null;
            public IBattlefield Battlefield => null;

            public event System.Action<ICombatState> OnStateChanged;
            public event System.Action<IPlayer> OnTurnStarted { add { } remove { } }
            public event System.Action<RoundPhase> OnRoundPhaseChanged { add { } remove { } }
            public event System.Action<IReadOnlyList<EnemyIntent>> OnEnemyPlansRevealed;
            public event System.Action<IPlayer, CombatPhase> OnGameEnded { add { } remove { } }

            public void Initialize(ICombatState initialState, IReadOnlyList<IPlayer> players) { }
            public void AddUnit(IUnit unit) { }
            public void BeginRounds() { }
            public bool ResolveNextEnemyIntent() => false;
            public ActionResult ProcessAction(IAction action) => null;
            public void Update() { }
            public void InitializeBattlefield(PlatformHexSurface surface, Vector3 center) { }
            public void CleanupBattlefield() { }

            public void RaiseStateChanged() => OnStateChanged?.Invoke(CombatState);
            public void RaisePlansRevealed() => OnEnemyPlansRevealed?.Invoke(CombatState.EnemyIntents);
        }

        private sealed class NoopAI : IAIDecisionMaker
        {
            public IAction DecideAction(ICombatState gameState, IUnit unit) =>
                new EndUnitTurnAction(unit.Owner, unit.Id);
        }

        private const int HeroId = 1;
        private const int EnemyId = 10;

        private HumanPlayer _human;
        private AIPlayer _enemyOwner;
        private FakeView _view;
        private FakeController _controller;
        private UnitPlanIconsPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _human = new HumanPlayer(1, "Player");
            _enemyOwner = new AIPlayer(100, "Enemy", new NoopAI());
            _view = new FakeView();
            _controller = new FakeController();
            _presenter = new UnitPlanIconsPresenter(_controller, _view);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
        }

        private static IAbilityInstance Ability(int id)
        {
            return new AbilityInstance(new DataDrivenDamageAbility(
                id, $"Ability {id}", 1, AbilityShapeData.ForLine(2), 10));
        }

        private CombatState StateWith(IReadOnlyList<EnemyIntent> intents = null, params IUnit[] units)
        {
            return new CombatState(units.ToList(),
                new List<IPlayer> { _human, _enemyOwner },
                _human, phase: CombatPhase.Combat,
                enemyIntents: intents);
        }

        [Test]
        public void PlayerUnit_IconsMirrorQueueOrder()
        {
            var first = Ability(100);
            var second = Ability(101);
            var hero = new Unit(HeroId, _human, new HexCoordinates(0, 0), 50, 50,
                new List<IAbilityInstance> { first, second },
                new List<ScheduledAbility>
                {
                    new ScheduledAbility(second, 1),
                    new ScheduledAbility(first, 0)
                });
            _controller.CombatState = StateWith(null, hero);

            _controller.RaiseStateChanged();

            var icons = _view.Icons[HeroId];
            Assert.AreEqual(2, icons.Count);
            Assert.AreEqual(100, icons[0].AbilityId, "execution order 0 first");
            Assert.AreEqual(101, icons[1].AbilityId);
            Assert.IsFalse(icons[0].IsEnemyIntent);
        }

        [Test]
        public void EnemyUnit_ShowsCommittedAbilityIntent_WhenPlansRevealed()
        {
            var ability = Ability(200);
            var enemy = new Unit(EnemyId, _enemyOwner, new HexCoordinates(2, 0), 30, 30,
                new List<IAbilityInstance> { ability });
            var intent = new EnemyIntent(
                EnemyId,
                new ScheduleAbilityAction(_enemyOwner, EnemyId, 200, HexDirection.W),
                HexDirection.W, enemy.Position, new List<HexCoordinates>());
            _controller.CombatState = StateWith(new List<EnemyIntent> { intent }, enemy);

            _controller.RaisePlansRevealed();

            var icons = _view.Icons[EnemyId];
            Assert.AreEqual(1, icons.Count);
            Assert.AreEqual(200, icons[0].AbilityId);
            Assert.IsTrue(icons[0].IsEnemyIntent);
            Assert.IsFalse(icons[0].IsMoveIntent);
        }

        [Test]
        public void EnemyUnit_ShowsMoveGlyph_ForCommittedMove()
        {
            var enemy = new Unit(EnemyId, _enemyOwner, new HexCoordinates(2, 0), 30, 30,
                new List<IAbilityInstance>());
            var intent = new EnemyIntent(
                EnemyId,
                new MoveAction(_enemyOwner, EnemyId, new HexCoordinates(1, 0)),
                null, enemy.Position, null);
            _controller.CombatState = StateWith(new List<EnemyIntent> { intent }, enemy);

            _controller.RaisePlansRevealed();

            var icons = _view.Icons[EnemyId];
            Assert.AreEqual(1, icons.Count);
            Assert.IsTrue(icons[0].IsMoveIntent);
        }

        [Test]
        public void DeadUnit_ShowsNoIcons()
        {
            var enemy = new Unit(EnemyId, _enemyOwner, new HexCoordinates(2, 0), 0, 30,
                new List<IAbilityInstance>());
            var intent = new EnemyIntent(
                EnemyId,
                new MoveAction(_enemyOwner, EnemyId, new HexCoordinates(1, 0)),
                null, enemy.Position, null);
            _controller.CombatState = StateWith(new List<EnemyIntent> { intent }, enemy);

            _controller.RaiseStateChanged();

            Assert.IsEmpty(_view.Icons[EnemyId]);
        }

        [Test]
        public void ExecutedQueue_ClearsThePlayerRow()
        {
            var ability = Ability(100);
            var heroWithQueue = new Unit(HeroId, _human, new HexCoordinates(0, 0), 50, 50,
                new List<IAbilityInstance> { ability },
                new List<ScheduledAbility> { new ScheduledAbility(ability, 0) });
            _controller.CombatState = StateWith(null, heroWithQueue);
            _controller.RaiseStateChanged();
            Assert.AreEqual(1, _view.Icons[HeroId].Count);

            var heroAfterExecution = heroWithQueue.WithAbilityQueue(new List<ScheduledAbility>());
            _controller.CombatState = StateWith(null, heroAfterExecution);
            _controller.RaiseStateChanged();

            Assert.IsEmpty(_view.Icons[HeroId]);
        }
    }
}
