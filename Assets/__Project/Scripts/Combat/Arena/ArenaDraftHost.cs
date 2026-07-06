using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Arena.Core;
using Core.Logging;
using Zenject;

namespace Combat.Arena
{
    /// <summary>
    /// The hosting machine's draft authority (mirrors <see cref="ArenaMatchHost"/>): composes
    /// the board from the floor + the participants' catalog union, opens the draft, validates
    /// pick requests against its own model, and broadcasts every canonically applied pick. Pick
    /// deadlines are host-only policy (G4 req 13): humans get the generous soft timer, AI seats
    /// a short pacing delay, departed seats fill immediately — all through the same
    /// deterministic auto-pick, so the draft can never hang. Every replica (the host's own
    /// <see cref="ArenaDraftFlow"/> included) advances on the broadcasts, never on this model.
    /// </summary>
    public class ArenaDraftHost : ITickable, IDisposable
    {
        private readonly IArenaTransport _transport;
        private readonly ArenaTastedCatalogRegistry _catalogRegistry;
        private readonly IArenaDraftPartInfoSource _partInfoSource;
        private readonly ArenaDraftSettings _settings;
        private readonly IArenaDraftClock _clock;
        private readonly IGameLogger _logger;

        private readonly HashSet<int> _aiPlayerIds = new HashSet<int>();
        private readonly HashSet<int> _departedPlayerIds = new HashSet<int>();
        private readonly List<int> _pendingDeparted = new List<int>();

        private ArenaDraftModel _model;
        private float _pickDeadline;
        private bool _active;

        public ArenaDraftHost(
            IArenaTransport transport,
            ArenaTastedCatalogRegistry catalogRegistry,
            IArenaDraftPartInfoSource partInfoSource,
            ArenaDraftSettings settings,
            IArenaDraftClock clock,
            IGameLogger logger)
        {
            _transport = transport;
            _catalogRegistry = catalogRegistry;
            _partInfoSource = partInfoSource;
            _settings = settings;
            _clock = clock;
            _logger = logger;
        }

        public void Dispose()
        {
            Deactivate();
        }

        /// <summary>
        /// Host-only: composes the board and opens the draft. Seats draft in ascending PlayerId
        /// order (the same canonical order the roster broadcast uses).
        /// </summary>
        public void StartDraft(
            int matchSeed,
            IReadOnlyList<ArenaRosterSlot> roster,
            IReadOnlyCollection<int> aiPlayerIds)
        {
            var seats = roster.OrderBy(s => s.PlayerId).ToList();
            var catalogUnion = ResolveCatalogUnion(seats.Select(s => s.ClientId));

            var board = ArenaDraftBoardComposer.Compose(
                matchSeed,
                _settings.FloorParts,
                catalogUnion,
                _settings.SlotLoadout,
                _settings.CatalogSampleSize);

            _model = new ArenaDraftModel(
                board, seats.Select(s => s.PlayerId).ToList(), _settings.SlotLoadout);

            _aiPlayerIds.Clear();
            if (aiPlayerIds != null)
            {
                _aiPlayerIds.UnionWith(aiPlayerIds);
            }

            _departedPlayerIds.Clear();
            _pendingDeparted.Clear();

            _transport.DraftPickRequested += HandlePickRequested;
            _transport.PlayerDeparted += HandlePlayerDeparted;
            _active = true;

            _logger.Info(LogCategory.Combat,
                $"[ArenaDraftHost] Draft open: {seats.Count} seats, {board.Count} board entries " +
                $"({catalogUnion.Count} catalog-sampled candidates offered)");

            _transport.BroadcastDraftStart(
                new ArenaDraftStart(_settings.PickTimerSeconds, _settings.SlotLoadout, board));
            ArmDeadline();
        }

        public void Tick()
        {
            if (!_active || _model == null || _model.IsComplete)
            {
                return;
            }

            if (_clock.Now < _pickDeadline)
            {
                return;
            }

            int picker = _model.CurrentPickerPlayerId;
            var pick = new ArenaDraftPick(
                _model.PickIndex, picker, _model.AutoPickEntryFor(picker), wasAutoPick: true);
            ApplyAndBroadcast(pick);
        }

        private void HandlePickRequested(ArenaDraftPick pick)
        {
            if (!_active || _model == null)
            {
                return;
            }

            // Validation happens implicitly: TryApply is the single legality gate.
            ApplyAndBroadcast(pick);
        }

        private void HandlePlayerDeparted(int playerId)
        {
            if (!_active || _model == null || !_model.SeatOrder.Contains(playerId))
            {
                return;
            }

            if (_departedPlayerIds.Add(playerId))
            {
                _pendingDeparted.Add(playerId);
                _logger.Info(LogCategory.Combat,
                    $"[ArenaDraftHost] Player {playerId} departed mid-draft; their seat auto-picks");
            }

            if (!_model.IsComplete && _model.CurrentPickerPlayerId == playerId)
            {
                ArmDeadline();
            }
        }

        private void ApplyAndBroadcast(ArenaDraftPick pick)
        {
            var result = _model.TryApply(pick);
            if (result != ArenaDraftPickResult.Applied)
            {
                _logger.Warning(LogCategory.Combat,
                    $"[ArenaDraftHost] Rejected pick (player {pick.PlayerId}, entry {pick.EntryId}, " +
                    $"index {pick.PickIndex}): {result}");
                return;
            }

            var departed = new List<int>(_pendingDeparted);
            _pendingDeparted.Clear();
            _transport.BroadcastDraftPick(new ArenaDraftPickApplied(pick, departed));

            if (_model.IsComplete)
            {
                Deactivate();
            }
            else
            {
                ArmDeadline();
            }
        }

        private void ArmDeadline()
        {
            int picker = _model.CurrentPickerPlayerId;
            float delay = _departedPlayerIds.Contains(picker)
                ? 0f
                : _aiPlayerIds.Contains(picker)
                    ? _settings.AiPickDelaySeconds
                    : _settings.PickTimerSeconds;
            _pickDeadline = _clock.Now + delay;
        }

        private IReadOnlyList<ArenaDraftPartInfo> ResolveCatalogUnion(IEnumerable<ulong> clientIds)
        {
            var infos = new List<ArenaDraftPartInfo>();
            foreach (var partId in _catalogRegistry.UnionFor(clientIds))
            {
                if (_partInfoSource.TryGet(partId, out var info))
                {
                    infos.Add(info);
                }
            }

            return infos;
        }

        private void Deactivate()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            _transport.DraftPickRequested -= HandlePickRequested;
            _transport.PlayerDeparted -= HandlePlayerDeparted;
        }
    }
}
