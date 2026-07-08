using System;
using System.Collections.Generic;
using Combat.Controller;
using Combat.Core;
using Combat.View;

namespace Combat.Player
{
    /// <summary>
    /// Builds the turn-order strip's ordered actor list (D2) and pushes it to the view. Reads the same
    /// live combat state the round runs on: the player and each live enemy, ordered leader-first per the
    /// round's initiative (<see cref="RoundLeadPolicy"/> over the controller's opening initiator), with
    /// the current side and any already-acted side marked. Rebuilds on every state / phase change so the
    /// strip tracks units acting and leaving. Pure C# — the view is a thin adapter.
    /// </summary>
    public sealed class TurnOrderStripPresenter : IDisposable
    {
        private readonly ICombatController _combatController;
        private readonly ITurnOrderStripView _view;
        private readonly CombatRuleModifiers _rules;

        public TurnOrderStripPresenter(ICombatController combatController, ITurnOrderStripView view,
            CombatRuleModifiers rules = null)
        {
            _combatController = combatController ?? throw new ArgumentNullException(nameof(combatController));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _rules = rules ?? CombatRuleModifiers.Neutral;

            _combatController.OnStateChanged += HandleStateChanged;
            _combatController.OnRoundPhaseChanged += HandleRoundPhaseChanged;
            _combatController.OnEnemyPlansRevealed += HandlePlansRevealed;
            _combatController.OnGameEnded += HandleGameEnded;

            Rebuild(_combatController.CombatState);
        }

        public void Dispose()
        {
            _combatController.OnStateChanged -= HandleStateChanged;
            _combatController.OnRoundPhaseChanged -= HandleRoundPhaseChanged;
            _combatController.OnEnemyPlansRevealed -= HandlePlansRevealed;
            _combatController.OnGameEnded -= HandleGameEnded;
            _view.Clear();
        }

        private void HandleStateChanged(ICombatState state) => Rebuild(state);
        private void HandleRoundPhaseChanged(RoundPhase phase) => Rebuild(_combatController.CombatState);
        private void HandlePlansRevealed(IReadOnlyList<EnemyIntent> intents) => Rebuild(_combatController.CombatState);

        private void HandleGameEnded(IPlayer winner, CombatPhase phase) => _view.Clear();

        /// <summary>
        /// Recomputes the strip from live state: the leader's side first, current / already-acted marks
        /// derived from the round phase and the lead, dead units dropped.
        /// </summary>
        private void Rebuild(ICombatState state)
        {
            if (state == null)
            {
                _view.Clear();
                return;
            }

            // Must consult the same rule the flow does (Track Y), or the strip lies about initiative.
            bool enemyLeads = RoundLeadPolicy.EnemyLeadsThisRound(
                state.TurnNumber, _combatController.OpeningInitiator, _rules.EnemiesAlwaysLead);
            ResolveSideStatus(state.RoundPhase, enemyLeads,
                out bool playerCurrent, out bool enemyCurrent, out bool playerActed, out bool enemyActed);

            var players = BuildPlayerEntries(state, playerCurrent, playerActed);
            var enemies = BuildEnemyEntries(state, enemyCurrent, enemyActed);

            var entries = new List<TurnOrderEntryModel>(players.Count + enemies.Count);
            if (enemyLeads)
            {
                entries.AddRange(enemies);
                entries.AddRange(players);
            }
            else
            {
                entries.AddRange(players);
                entries.AddRange(enemies);
            }

            _view.SetEntries(entries);
        }

        /// <summary>
        /// Maps the round phase + lead to which side is acting now and which already resolved its phase:
        /// the lead side acts first, the other second, so the "already acted" flag is fully determined.
        /// </summary>
        private static void ResolveSideStatus(RoundPhase phase, bool enemyLeads,
            out bool playerCurrent, out bool enemyCurrent, out bool playerActed, out bool enemyActed)
        {
            switch (phase)
            {
                case RoundPhase.PlayerAct:
                    playerCurrent = true;
                    enemyCurrent = false;
                    playerActed = false;
                    enemyActed = enemyLeads; // enemies already resolved if they led the round
                    break;
                case RoundPhase.EnemyResolve:
                    playerCurrent = false;
                    enemyCurrent = true;
                    playerActed = !enemyLeads; // the player already acted if the player led
                    enemyActed = false;
                    break;
                default: // EnemyPlan — nobody has acted yet; the leader is up next
                    playerCurrent = !enemyLeads;
                    enemyCurrent = enemyLeads;
                    playerActed = false;
                    enemyActed = false;
                    break;
            }
        }

        private static List<TurnOrderEntryModel> BuildPlayerEntries(ICombatState state, bool isCurrent, bool hasActed)
        {
            var entries = new List<TurnOrderEntryModel>();
            foreach (var unit in state.Units)
            {
                if (unit.IsAlive && unit.Owner.Type == PlayerType.Human)
                {
                    entries.Add(new TurnOrderEntryModel(unit.Id, NameOf(unit), isPlayer: true, isCurrent, hasActed));
                }
            }

            return entries;
        }

        /// <summary>
        /// Live enemies in <b>resolution order</b> — the order committed intents fire — dropping any that
        /// died. Falls back to any alive AI unit not carrying an intent (defensive; every alive AI plans).
        /// </summary>
        private static List<TurnOrderEntryModel> BuildEnemyEntries(ICombatState state, bool isCurrent, bool hasActed)
        {
            var entries = new List<TurnOrderEntryModel>();
            var seen = new HashSet<int>();

            foreach (var intent in state.EnemyIntents)
            {
                var unit = state.GetUnit(intent.UnitId);
                if (unit != null && unit.IsAlive && seen.Add(unit.Id))
                {
                    entries.Add(new TurnOrderEntryModel(unit.Id, NameOf(unit), isPlayer: false, isCurrent, hasActed));
                }
            }

            foreach (var unit in state.Units)
            {
                if (unit.IsAlive && unit.Owner.Type == PlayerType.AI && seen.Add(unit.Id))
                {
                    entries.Add(new TurnOrderEntryModel(unit.Id, NameOf(unit), isPlayer: false, isCurrent, hasActed));
                }
            }

            return entries;
        }

        private static string NameOf(IUnit unit)
        {
            var name = unit.Owner?.Name;
            return string.IsNullOrEmpty(name) ? $"Unit {unit.Id}" : name;
        }
    }
}
