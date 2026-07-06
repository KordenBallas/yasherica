using System;
using System.Collections.Generic;
using Combat.Controller;
using Combat.Core;

namespace Combat.Player
{
    /// <summary>
    /// Builds the enemy committed-intent board telegraph (D3): for every live enemy holding a committed
    /// intent this round it emits an <see cref="EnemyIntentTelegraphModel"/> — armed (→ ready pose +
    /// restless icons) and, for a committed move, the from/to cells (→ direction arrow). Reads the same
    /// <c>EnemyIntents</c> the round runs on, rebuilding on every reveal / state change so the cue clears
    /// the moment an enemy's intent resolves (uniform across all armed enemies, no turn-order escalation).
    /// Pure C# — the view is a thin adapter.
    /// </summary>
    public sealed class EnemyIntentTelegraphPresenter : IDisposable
    {
        private readonly ICombatController _combatController;
        private readonly IEnemyIntentTelegraphView _view;

        public EnemyIntentTelegraphPresenter(ICombatController combatController, IEnemyIntentTelegraphView view)
        {
            _combatController = combatController ?? throw new ArgumentNullException(nameof(combatController));
            _view = view ?? throw new ArgumentNullException(nameof(view));

            _combatController.OnStateChanged += HandleStateChanged;
            _combatController.OnEnemyPlansRevealed += HandlePlansRevealed;
            _combatController.OnGameEnded += HandleGameEnded;

            Rebuild(_combatController.CombatState);
        }

        public void Dispose()
        {
            _combatController.OnStateChanged -= HandleStateChanged;
            _combatController.OnEnemyPlansRevealed -= HandlePlansRevealed;
            _combatController.OnGameEnded -= HandleGameEnded;
            _view.Clear();
        }

        /// <summary>The per-round armed enemies (for tests + the view). Alive AI units with a committed intent.</summary>
        public static IReadOnlyList<EnemyIntentTelegraphModel> BuildTelegraphs(ICombatState state)
        {
            var models = new List<EnemyIntentTelegraphModel>();
            if (state == null)
                return models;

            // The telegraph reads during the PLAN phase only. Once the round enters EnemyResolve the
            // enemies act and move, so the arrow + wind-up pose clear — otherwise the pose would pin the
            // enemy's transform and fight its movement (EnemyIntents persists across the resolve phase).
            if (state.RoundPhase == RoundPhase.EnemyResolve)
                return models;

            foreach (var intent in state.EnemyIntents)
            {
                var unit = state.GetUnit(intent.UnitId);
                if (unit == null || !unit.IsAlive || unit.Owner.Type != PlayerType.AI)
                    continue;

                bool hasMove = intent.IsMove && intent.MoveDestination.HasValue;
                models.Add(new EnemyIntentTelegraphModel(
                    intent.UnitId,
                    isArmed: true,
                    hasMove,
                    intent.CommittedOrigin,
                    hasMove ? intent.MoveDestination.Value : intent.CommittedOrigin));
            }

            return models;
        }

        private void HandleStateChanged(ICombatState state) => Rebuild(state);
        private void HandlePlansRevealed(IReadOnlyList<EnemyIntent> intents) => Rebuild(_combatController.CombatState);
        private void HandleGameEnded(IPlayer winner, CombatPhase phase) => _view.Clear();

        private void Rebuild(ICombatState state)
        {
            if (state == null)
            {
                _view.Clear();
                return;
            }

            _view.SetTelegraphs(BuildTelegraphs(state));
        }
    }
}
