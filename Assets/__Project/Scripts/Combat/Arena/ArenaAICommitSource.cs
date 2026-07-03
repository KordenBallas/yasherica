using System;
using System.Linq;
using Combat.Arena.Core;
using Combat.Controller;
using Combat.Core;
using Combat.Player;
using Core.Logging;

namespace Combat.Arena
{
    /// <summary>
    /// Offline dummies: at each Planning phase, every AI-owned unit decides its action (the same
    /// seeded decision makers PvE uses) and locks it in through the same commit path as a human —
    /// so the offline arena exercises the exact symmetric round the networked one runs.
    /// </summary>
    public class ArenaAICommitSource : IDisposable
    {
        private readonly ArenaCommitBuilder _commitBuilder;
        private readonly IArenaTransport _transport;
        private readonly IGameLogger _logger;

        private ICombatController _controller;

        public ArenaAICommitSource(
            ArenaCommitBuilder commitBuilder,
            IArenaTransport transport,
            IGameLogger logger)
        {
            _commitBuilder = commitBuilder;
            _transport = transport;
            _logger = logger;
        }

        public void Initialize(ICombatController controller)
        {
            _controller = controller;
            _controller.OnRoundPhaseChanged += HandleRoundPhaseChanged;
        }

        public void Dispose()
        {
            if (_controller != null)
            {
                _controller.OnRoundPhaseChanged -= HandleRoundPhaseChanged;
                _controller = null;
            }
        }

        private void HandleRoundPhaseChanged(RoundPhase phase)
        {
            if (phase != RoundPhase.PlayerAct)
                return;

            var state = _controller.CombatState;
            var aiUnits = state.Units
                .Where(u => u.IsAlive && u.Owner is AIPlayer)
                .OrderBy(u => u.Id)
                .ToList();

            foreach (var unit in aiUnits)
            {
                var ai = (AIPlayer)unit.Owner;
                var action = ai.RequestAction(state, unit);
                if (action == null)
                {
                    _logger.Warning(LogCategory.Combat,
                        $"[ArenaAICommitSource] AI returned null action for unit {unit.Id}; committing a pass");
                    action = new EndUnitTurnAction(unit.Owner, unit.Id);
                }

                var commit = _commitBuilder.FromAiAction(state, unit, action);
                _transport.SubmitCommit(new ArenaCommitEnvelope(state.TurnNumber, commit, 0));
            }
        }
    }
}
