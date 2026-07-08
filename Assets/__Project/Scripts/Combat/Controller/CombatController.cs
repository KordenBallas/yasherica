using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using Combat.Core.StatusEffects;
using Combat.Execution;
using Combat.TurnManagement;
using Core.Logging;
using System.Collections.Generic;
using UnityEngine;

namespace Combat.Controller
{
    /// <summary>
    /// The PvE combat controller: a thin <see cref="ICombatController"/> adapter over the shared
    /// <see cref="CombatRoundEngine"/> configured with the PvE round shape (<see cref="PveCombatFlow"/>) and
    /// the eliminate-all-enemies win condition. All round/state mechanics live in the engine — this class
    /// only composes the engine + flow and forwards the seam (A1 reconvergence with the Arena controller).
    /// The constructor signature is preserved so every existing construction site (CombatControllerFactory,
    /// installers) is unchanged; the engine and flow are internal composition detail, mirroring the prior
    /// pattern where <see cref="RoundLifecycleProcessor"/> was built internally.
    /// </summary>
    public class CombatController : ICombatController
    {
        private readonly CombatRoundEngine _engine;

        public CombatController(
            IActionValidator actionValidator,
            IActionExecutor actionExecutor,
            ITurnManager turnManager,
            StatusEffectTriggerProcessor triggerProcessor,
            EnemyIntentPlanner intentPlanner,
            EnemyIntentResolver intentResolver,
            BattlefieldFactory battlefieldFactory,
            HexDirectionConfig hexConfig,
            IGameLogger logger,
            CombatRuleModifiers rules = null)
        {
            var flow = new PveCombatFlow(intentPlanner, hexConfig, rules);
            _engine = new CombatRoundEngine(
                actionValidator,
                actionExecutor,
                turnManager,
                intentResolver,
                // Built internally (not injected) so the extraction stays signature-preserving for every
                // existing construction site; Arena binds its own instance in its installer.
                new RoundLifecycleProcessor(triggerProcessor),
                battlefieldFactory,
                hexConfig,
                new IWinCondition[] { new EliminateAllEnemiesWinCondition() },
                flow,
                logger);
        }

        public ICombatState CombatState => _engine.State;
        public ITurnManager TurnManager => _engine.TurnManager;
        public IBattlefield Battlefield => _engine.Battlefield;
        public CombatInitiator OpeningInitiator => _engine.OpeningInitiator;

        public event System.Action<ICombatState> OnStateChanged
        {
            add => _engine.OnStateChanged += value;
            remove => _engine.OnStateChanged -= value;
        }
        public event System.Action<IPlayer> OnTurnStarted
        {
            add => _engine.OnTurnStarted += value;
            remove => _engine.OnTurnStarted -= value;
        }
        public event System.Action<RoundPhase> OnRoundPhaseChanged
        {
            add => _engine.OnRoundPhaseChanged += value;
            remove => _engine.OnRoundPhaseChanged -= value;
        }
        public event System.Action<IReadOnlyList<EnemyIntent>> OnEnemyPlansRevealed
        {
            add => _engine.OnEnemyPlansRevealed += value;
            remove => _engine.OnEnemyPlansRevealed -= value;
        }
        public event System.Action<IPlayer, CombatPhase> OnGameEnded
        {
            add => _engine.OnGameEnded += value;
            remove => _engine.OnGameEnded -= value;
        }

        public void Initialize(ICombatState initialState, IReadOnlyList<IPlayer> players,
            CombatInitiator openingInitiator = CombatInitiator.Enemy)
            => _engine.Initialize(initialState, players, openingInitiator);

        public void AddUnit(IUnit unit) => _engine.AddUnit(unit);

        public void BeginRounds() => _engine.BeginRounds();

        public bool ResolveNextEnemyIntent() => _engine.ResolveNextEnemyIntent();

        public ActionResult ProcessAction(IAction action) => _engine.ProcessAction(action);

        public void Update() => _engine.Update();

        public void InitializeBattlefield(PlatformHexSurface surface, Vector3 center)
            => _engine.InitializeBattlefield(surface, center);

        public void CleanupBattlefield() => _engine.CleanupBattlefield();
    }
}
