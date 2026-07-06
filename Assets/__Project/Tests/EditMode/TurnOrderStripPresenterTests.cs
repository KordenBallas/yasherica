using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Controller;
using Combat.Core;
using Combat.Execution;
using Combat.Player;
using Combat.TurnManagement;
using Combat.View;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Proves the D2 turn-order strip models: actors ordered leader-first per the round's initiative,
    /// enemies in resolution (EnemyIntents) order, dead units dropped, and the current / already-acted
    /// side derived from the round phase + lead. Only the opening round is initiator-led.
    /// </summary>
    [TestFixture]
    public class TurnOrderStripPresenterTests
    {
        private sealed class FakeView : ITurnOrderStripView
        {
            public IReadOnlyList<TurnOrderEntryModel> Entries = new List<TurnOrderEntryModel>();
            public bool Cleared;

            public void SetEntries(IReadOnlyList<TurnOrderEntryModel> entries)
            {
                Entries = entries;
                Cleared = false; // populating the strip un-clears it (the ctor's null-state Rebuild clears)
            }

            public void Clear() => Cleared = true;
        }

        private sealed class FakeController : ICombatController
        {
            public ICombatState CombatState { get; set; }
            public ITurnManager TurnManager => null;
            public IBattlefield Battlefield => null;
            public CombatInitiator OpeningInitiator { get; set; } = CombatInitiator.Enemy;

            public event System.Action<ICombatState> OnStateChanged;
            public event System.Action<IPlayer> OnTurnStarted { add { } remove { } }
            public event System.Action<RoundPhase> OnRoundPhaseChanged;
            public event System.Action<IReadOnlyList<EnemyIntent>> OnEnemyPlansRevealed { add { } remove { } }
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
            public void RaiseRoundPhaseChanged(RoundPhase phase) => OnRoundPhaseChanged?.Invoke(phase);
            public void RaiseGameEnded() => OnGameEnded?.Invoke(null, CombatPhase.Victory);
        }

        private const int HeroId = 1;
        private const int EnemyA = 10;
        private const int EnemyB = 11;

        private HumanPlayer _human;
        private AIPlayer _enemyOwner;
        private FakeView _view;
        private FakeController _controller;
        private TurnOrderStripPresenter _presenter;

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
            _view = new FakeView();
            _controller = new FakeController();
            _presenter = new TurnOrderStripPresenter(_controller, _view);
        }

        [TearDown]
        public void TearDown() => _presenter.Dispose();

        private Unit Hero() =>
            new Unit(HeroId, _human, new HexCoordinates(0, 0), 50, 50, new List<IAbilityInstance>());

        private Unit Enemy(int id, int hp = 30) =>
            new Unit(id, _enemyOwner, new HexCoordinates(2, 0), hp, 30, new List<IAbilityInstance>());

        private static EnemyIntent MoveIntent(AIPlayer owner, int unitId) =>
            new EnemyIntent(unitId, new MoveAction(owner, unitId, new HexCoordinates(1, 0)),
                null, new HexCoordinates(2, 0), null);

        private CombatState State(int turnNumber, RoundPhase phase,
            IReadOnlyList<EnemyIntent> intents, params IUnit[] units) =>
            new CombatState(units.ToList(), new List<IPlayer> { _human, _enemyOwner }, _human,
                turnNumber: turnNumber, phase: CombatPhase.Combat, roundPhase: phase, enemyIntents: intents);

        [Test]
        public void PlayerLed_OpeningRound_PlayerListedFirstAndCurrent()
        {
            _controller.OpeningInitiator = CombatInitiator.Player;
            var hero = Hero();
            var enemy = Enemy(EnemyA);
            _controller.CombatState = State(1, RoundPhase.PlayerAct,
                new List<EnemyIntent> { MoveIntent(_enemyOwner, EnemyA) }, hero, enemy);

            _controller.RaiseStateChanged();

            var entries = _view.Entries;
            Assert.AreEqual(2, entries.Count);
            Assert.IsTrue(entries[0].IsPlayer, "player leads a player-initiated round");
            Assert.IsTrue(entries[0].IsCurrent, "the player is acting");
            Assert.IsFalse(entries[1].IsPlayer);
            Assert.IsFalse(entries[1].IsCurrent);
        }

        [Test]
        public void EnemyLed_OpeningRound_EnemiesListedFirstAndCurrent()
        {
            _controller.OpeningInitiator = CombatInitiator.Enemy;
            var hero = Hero();
            var enemy = Enemy(EnemyA);
            _controller.CombatState = State(1, RoundPhase.EnemyResolve,
                new List<EnemyIntent> { MoveIntent(_enemyOwner, EnemyA) }, hero, enemy);

            _controller.RaiseStateChanged();

            var entries = _view.Entries;
            Assert.AreEqual(2, entries.Count);
            Assert.IsFalse(entries[0].IsPlayer, "enemy leads an enemy-initiated opening round");
            Assert.IsTrue(entries[0].IsCurrent, "the enemy is resolving");
            Assert.IsTrue(entries[1].IsPlayer);
            Assert.IsFalse(entries[1].IsCurrent, "the player has not acted yet");
        }

        [Test]
        public void EnemyLed_PlayerActPhase_EnemiesAlreadyActed()
        {
            _controller.OpeningInitiator = CombatInitiator.Enemy;
            var hero = Hero();
            var enemy = Enemy(EnemyA);
            _controller.CombatState = State(1, RoundPhase.PlayerAct,
                new List<EnemyIntent> { MoveIntent(_enemyOwner, EnemyA) }, hero, enemy);

            _controller.RaiseStateChanged();

            var enemyEntry = _view.Entries.First(e => !e.IsPlayer);
            var playerEntry = _view.Entries.First(e => e.IsPlayer);
            Assert.IsTrue(enemyEntry.HasActed, "enemies resolved first in an enemy-led round");
            Assert.IsTrue(playerEntry.IsCurrent, "the player acts after the enemy lead");
        }

        [Test]
        public void EnemyOrder_FollowsResolutionOrder()
        {
            _controller.OpeningInitiator = CombatInitiator.Player;
            var hero = Hero();
            var enemyA = Enemy(EnemyA);
            var enemyB = Enemy(EnemyB);
            // Intents list order B-then-A is the resolution order the strip must mirror.
            _controller.CombatState = State(1, RoundPhase.PlayerAct,
                new List<EnemyIntent> { MoveIntent(_enemyOwner, EnemyB), MoveIntent(_enemyOwner, EnemyA) },
                hero, enemyA, enemyB);

            _controller.RaiseStateChanged();

            var enemyEntries = _view.Entries.Where(e => !e.IsPlayer).ToList();
            Assert.AreEqual(EnemyB, enemyEntries[0].UnitId);
            Assert.AreEqual(EnemyA, enemyEntries[1].UnitId);
        }

        [Test]
        public void DeadEnemy_IsDropped()
        {
            _controller.OpeningInitiator = CombatInitiator.Player;
            var hero = Hero();
            var deadEnemy = Enemy(EnemyA, hp: 0);
            _controller.CombatState = State(1, RoundPhase.PlayerAct,
                new List<EnemyIntent> { MoveIntent(_enemyOwner, EnemyA) }, hero, deadEnemy);

            _controller.RaiseStateChanged();

            Assert.IsFalse(_view.Entries.Any(e => e.UnitId == EnemyA), "a dead enemy drops out of the strip");
            Assert.AreEqual(1, _view.Entries.Count);
        }

        [Test]
        public void LaterRound_EvenIfEnemyInitiated_PlayerLeads()
        {
            _controller.OpeningInitiator = CombatInitiator.Enemy;
            var hero = Hero();
            var enemy = Enemy(EnemyA);
            // Round 2: the initiator no longer leads (multi-round policy out of scope).
            _controller.CombatState = State(2, RoundPhase.PlayerAct,
                new List<EnemyIntent> { MoveIntent(_enemyOwner, EnemyA) }, hero, enemy);

            _controller.RaiseStateChanged();

            Assert.IsTrue(_view.Entries[0].IsPlayer, "round 2 is player-led regardless of the initiator");
        }

        [Test]
        public void GameEnded_ClearsTheStrip()
        {
            _controller.CombatState = State(1, RoundPhase.PlayerAct, null, Hero());
            _controller.RaiseStateChanged();
            Assert.IsFalse(_view.Cleared);

            _controller.RaiseGameEnded();

            Assert.IsTrue(_view.Cleared, "the strip clears when combat ends");
        }
    }
}
