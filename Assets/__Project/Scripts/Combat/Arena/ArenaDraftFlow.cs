using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Arena.Core;
using Combat.Core;
using Core.Logging;

namespace Combat.Arena
{
    /// <summary>
    /// Every client's draft coordinator (host included): holds the local replica of the draft,
    /// advanced exclusively by the host's applied-pick broadcasts (never by local input — the
    /// presenter only *requests* picks through the transport). When the replica completes and
    /// the presenter confirms the "your monster" beat, hands the drafted loadouts to the match
    /// build. Pure C#; a draft-start arriving before the seats are known is buffered.
    /// </summary>
    public class ArenaDraftFlow : IDisposable
    {
        private readonly IArenaTransport _transport;
        private readonly IGameLogger _logger;
        private readonly List<int> _departedPlayerIds = new List<int>();
        private readonly Dictionary<int, string> _playerNamesById = new Dictionary<int, string>();

        private ArenaDraftStart _bufferedStart;
        private bool _prepared;
        private bool _resultDelivered;

        /// <summary>Raised when the replica is built and the draft screen should open.</summary>
        public event Action DraftOpened;

        /// <summary>Raised after every applied pick reaches the replica.</summary>
        public event Action<ArenaDraftPick> PickApplied;

        /// <summary>Raised when every seat has filled every slot (the "your monster" beat).</summary>
        public event Action DraftCompleted;

        /// <summary>Raised once the beat is confirmed: the match build may consume the result.</summary>
        public event Action<ArenaDraftResult> ReadyForCombat;

        public ArenaDraftFlow(IArenaTransport transport, IGameLogger logger)
        {
            _transport = transport;
            _logger = logger;
            _transport.DraftStartReceived += HandleDraftStart;
            _transport.DraftPickApplied += HandlePickApplied;
        }

        public void Dispose()
        {
            _transport.DraftStartReceived -= HandleDraftStart;
            _transport.DraftPickApplied -= HandlePickApplied;
        }

        public ArenaDraftModel Model { get; private set; }

        public float PickTimerSeconds { get; private set; }

        public int LocalPlayerId { get; private set; }

        public bool IsDraftComplete => Model != null && Model.IsComplete;

        public string PlayerNameOf(int playerId) =>
            _playerNamesById.TryGetValue(playerId, out var name) ? name : $"Player {playerId}";

        /// <summary>
        /// Called once the seats are known (setup received / offline players seated) — the
        /// replica needs player identities before it can open.
        /// </summary>
        public void PrepareForDraft(IReadOnlyList<IPlayer> players)
        {
            _playerNamesById.Clear();
            foreach (var player in players)
            {
                _playerNamesById[player.Id] = player.Name;
                if (player.Type == PlayerType.Human)
                {
                    LocalPlayerId = player.Id;
                }
            }

            _prepared = true;
            if (_bufferedStart != null)
            {
                var start = _bufferedStart;
                _bufferedStart = null;
                OpenDraft(start);
            }
        }

        /// <summary>The presenter's confirmation of the completion beat.</summary>
        public void ConfirmCompletion()
        {
            if (!IsDraftComplete || _resultDelivered)
            {
                return;
            }

            _resultDelivered = true;
            var loadouts = Model.SeatOrder.ToDictionary(
                playerId => playerId,
                playerId => Model.LoadoutOf(playerId));
            ReadyForCombat?.Invoke(new ArenaDraftResult(loadouts, new List<int>(_departedPlayerIds)));
        }

        private void HandleDraftStart(ArenaDraftStart start)
        {
            if (!_prepared)
            {
                // ReliableSequenced keeps setup before draft-start on the wire, but the local
                // seat bookkeeping may still be mid-flight — hold the start until prepared.
                _bufferedStart = start;
                return;
            }

            OpenDraft(start);
        }

        private void OpenDraft(ArenaDraftStart start)
        {
            var seatOrder = _playerNamesById.Keys.OrderBy(id => id).ToList();
            Model = new ArenaDraftModel(start.Board, seatOrder, start.SlotLoadout);
            PickTimerSeconds = start.PickTimerSeconds;
            _departedPlayerIds.Clear();
            _resultDelivered = false;

            _logger.Info(LogCategory.Combat,
                $"[ArenaDraftFlow] Draft opened: {seatOrder.Count} seats, {start.Board.Count} entries, " +
                $"{start.SlotLoadout.Count} slots to fill");
            DraftOpened?.Invoke();
        }

        private void HandlePickApplied(ArenaDraftPickApplied applied)
        {
            if (Model == null)
            {
                return;
            }

            foreach (var departedId in applied.DepartedPlayerIds)
            {
                if (!_departedPlayerIds.Contains(departedId))
                {
                    _departedPlayerIds.Add(departedId);
                }
            }

            var result = Model.TryApply(applied.Pick);
            if (result != ArenaDraftPickResult.Applied)
            {
                // A host-applied pick must always replay cleanly — anything else is divergence.
                _logger.Error(LogCategory.Combat,
                    $"[ArenaDraftFlow] REPLICA DIVERGENCE: host pick (player {applied.Pick.PlayerId}, " +
                    $"entry {applied.Pick.EntryId}, index {applied.Pick.PickIndex}) rejected locally: {result}");
                return;
            }

            PickApplied?.Invoke(applied.Pick);
            if (Model.IsComplete)
            {
                DraftCompleted?.Invoke();
            }
        }
    }
}
