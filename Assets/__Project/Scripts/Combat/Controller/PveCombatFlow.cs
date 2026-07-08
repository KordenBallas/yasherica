using Combat.Config;
using Combat.Core;
using Combat.Execution;
using Combat.TurnManagement;
using Core.Logging;

namespace Combat.Controller
{
    /// <summary>
    /// The PvE round shape over the shared <see cref="CombatRoundEngine"/>: enemies commit and reveal their
    /// intents at round start, the round's lead (the fight's initiator, D2) acts first, the player acts freely
    /// against the live board, and the committed enemy intents fire as shown. The two acting phases (player Act,
    /// enemy Resolve) run in the order the lead dictates and the round ends once both have happened.
    /// </summary>
    public class PveCombatFlow : ICombatRoundFlow
    {
        private readonly EnemyIntentPlanner _intentPlanner;
        private readonly HexDirectionConfig _hexConfig;
        private readonly CombatRuleModifiers _rules;

        // Per-round bookkeeping for the initiator-leads ordering (D2): the round runs its two acting phases
        // in the order the lead dictates; these track which of the two has already occurred this round.
        private bool _playerActedThisRound;
        private bool _enemiesResolvedThisRound;

        public PveCombatFlow(EnemyIntentPlanner intentPlanner, HexDirectionConfig hexConfig,
            CombatRuleModifiers rules = null)
        {
            _intentPlanner = intentPlanner;
            _hexConfig = hexConfig;
            _rules = rules ?? CombatRuleModifiers.Neutral;
        }

        public void OnInitialized(CombatRoundEngine engine)
        {
            // PvE has no bootstrap beyond the shared engine's.
        }

        /// <summary>
        /// Plan phase: every enemy commits and reveals its intent, then the round's lead acts first — the
        /// player's Act phase for a player-led round, or the enemy Resolve phase for an enemy-led opening
        /// round (D2).
        /// </summary>
        public void OnRoundStart(CombatRoundEngine engine)
        {
            _playerActedThisRound = false;
            _enemiesResolvedThisRound = false;
            engine.SetRoundPhase(RoundPhase.EnemyPlan);

            var intents = _intentPlanner.Plan(engine.State);
            // Reveal = orient (D6): every armed enemy turns toward its committed move/ability at plan time,
            // so all units read uniformly before the player acts.
            var planned = EnemyIntentFacingApplier.Apply(engine.State, intents, _hexConfig);
            planned = (planned as CombatState).WithEnemyIntents(intents);
            engine.SetState(planned);
            engine.RaiseEnemyPlansRevealed(intents);

            if (RoundLeadPolicy.EnemyLeadsThisRound(
                    engine.State.TurnNumber, engine.OpeningInitiator, _rules.EnemiesAlwaysLead))
            {
                // Enemy-initiated opening round: committed intents resolve before the player acts. Intents
                // are already in state, so this only flips the phase.
                engine.SetRoundPhase(RoundPhase.EnemyResolve);
                engine.RaiseStateChanged();
            }
            else
            {
                engine.BeginPlayerAct();
            }
        }

        public ActionResult ProcessAction(CombatRoundEngine engine, IAction action)
        {
            engine.Logger.Info(LogCategory.Combat, $"[PveCombatFlow] ProcessAction: Action={action.Type}, UnitId={action.UnitId}, ActionPlayerId={action.Player?.Id}");

            var validationResult = engine.Validator.ValidateDetailed(engine.State, action);
            if (!validationResult.IsValid)
            {
                return ActionResult.Failed(engine.State, validationResult.FailureReason);
            }

            var result = engine.Executor.ExecuteWithResult(engine.State, action);
            engine.SetState(result.NewState);
            engine.RaiseStateChanged();

            CheckTurnEnd(engine);
            engine.CheckWinConditions();

            return result;
        }

        public void OnIntentResolved(CombatRoundEngine engine, EnemyIntent intent)
        {
            // PvE resolves sequentially with the win check after every step — no batching.
        }

        /// <summary>
        /// The enemy Resolve phase has fired every committed intent. If the player has not yet acted this
        /// round (an enemy-led opening round), open the player's Act phase; otherwise the round is complete.
        /// This is the second half of the initiator-leads ordering (D2).
        /// </summary>
        public void OnEnemyResolveFinished(CombatRoundEngine engine)
        {
            _enemiesResolvedThisRound = true;

            if (!_playerActedThisRound)
                engine.BeginPlayerAct();
            else
                engine.EndRound();
        }

        public void OnRoundEndTail(CombatRoundEngine engine)
        {
            // PvE has no post-round tail (no lockstep hash).
        }

        public void OnCleanup(CombatRoundEngine engine)
        {
            // PvE has no teardown beyond the shared engine's.
        }

        /// <summary>
        /// When the player's active units are exhausted, close the Act phase: enter Resolve if the enemies
        /// have not resolved yet, otherwise (an enemy-led opening round already resolved) end the round.
        /// </summary>
        private void CheckTurnEnd(CombatRoundEngine engine)
        {
            if (engine.State.RoundPhase != RoundPhase.PlayerAct)
                return;

            var currentPlayer = engine.TurnManager.CurrentPlayer;
            var activeUnits = engine.State.GetActiveUnitsByPlayer(currentPlayer);

            if (activeUnits.Count == 0)
            {
                _playerActedThisRound = true;

                if (!_enemiesResolvedThisRound)
                {
                    engine.Logger.Info(LogCategory.Combat, "[PveCombatFlow] All player units have acted - entering Resolve phase");
                    engine.SetRoundPhase(RoundPhase.EnemyResolve);
                    engine.RaiseStateChanged();
                }
                else
                {
                    // Enemy-led opening round: enemies already resolved, so the player closing their Act
                    // ends the round (D2).
                    engine.Logger.Info(LogCategory.Combat, "[PveCombatFlow] Player acted after enemy-led resolve - ending round");
                    engine.EndRound();
                }
            }
        }
    }
}
