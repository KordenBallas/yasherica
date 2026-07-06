using System;
using System.Collections.Generic;
using System.Linq;

namespace Combat.Arena.Core
{
    /// <summary>
    /// The snake-draft state machine (P4-5 reqs 8–10): validates and applies picks, tracks
    /// per-player slot fills and board availability, and completes when every seat has filled
    /// every loadout slot. Holds no time and no transport — the pick deadline is host policy
    /// (ArenaDraftHost) and every replica advances only through applied picks (lockstep by
    /// construction).
    /// </summary>
    public class ArenaDraftModel
    {
        private readonly List<ArenaDraftBoardEntry> _board;
        private readonly Dictionary<int, ArenaDraftBoardEntry> _availableByEntryId;
        private readonly List<int> _seatPlayerIds;
        private readonly List<string> _slotLoadout;
        private readonly Dictionary<int, Dictionary<string, string>> _loadoutsByPlayerId;

        public event Action<ArenaDraftPick> PickApplied;
        public event Action Completed;

        public ArenaDraftModel(
            IReadOnlyList<ArenaDraftBoardEntry> board,
            IReadOnlyList<int> seatPlayerIdsInOrder,
            IReadOnlyList<string> slotLoadout)
        {
            if (seatPlayerIdsInOrder == null || seatPlayerIdsInOrder.Count == 0)
            {
                throw new ArgumentException("A draft needs at least one seat.", nameof(seatPlayerIdsInOrder));
            }

            if (slotLoadout == null || slotLoadout.Count == 0)
            {
                throw new ArgumentException("A draft needs at least one loadout slot.", nameof(slotLoadout));
            }

            _board = (board ?? Array.Empty<ArenaDraftBoardEntry>()).ToList();
            _availableByEntryId = _board.ToDictionary(e => e.EntryId);
            _seatPlayerIds = seatPlayerIdsInOrder.ToList();
            _slotLoadout = slotLoadout.ToList();
            _loadoutsByPlayerId = _seatPlayerIds.ToDictionary(
                id => id, _ => new Dictionary<string, string>(StringComparer.Ordinal));
        }

        public int PickIndex { get; private set; }

        public bool IsComplete => PickIndex >= _seatPlayerIds.Count * _slotLoadout.Count;

        public int CurrentPickerPlayerId =>
            _seatPlayerIds[ArenaSnakeOrder.SeatIndexAt(PickIndex, _seatPlayerIds.Count)];

        public IReadOnlyList<int> SeatOrder => _seatPlayerIds;

        public IReadOnlyList<string> SlotLoadout => _slotLoadout;

        public IReadOnlyList<ArenaDraftBoardEntry> Board => _board;

        public IEnumerable<ArenaDraftBoardEntry> AvailableEntries =>
            _board.Where(e => _availableByEntryId.ContainsKey(e.EntryId));

        public bool IsEntryAvailable(int entryId) => _availableByEntryId.ContainsKey(entryId);

        /// <summary>The slot → part id map a player has drafted so far.</summary>
        public IReadOnlyDictionary<string, string> LoadoutOf(int playerId) =>
            _loadoutsByPlayerId.TryGetValue(playerId, out var loadout)
                ? loadout
                : new Dictionary<string, string>();

        public ArenaDraftPickResult TryApply(ArenaDraftPick pick)
        {
            if (IsComplete)
            {
                return ArenaDraftPickResult.DraftComplete;
            }

            if (pick.PickIndex != PickIndex)
            {
                return ArenaDraftPickResult.StalePickIndex;
            }

            if (pick.PlayerId != CurrentPickerPlayerId)
            {
                return ArenaDraftPickResult.NotYourTurn;
            }

            if (!_availableByEntryId.TryGetValue(pick.EntryId, out var entry))
            {
                return ArenaDraftPickResult.EntryTaken;
            }

            var loadout = _loadoutsByPlayerId[pick.PlayerId];
            if (loadout.ContainsKey(entry.SlotId))
            {
                return ArenaDraftPickResult.SlotAlreadyFilled;
            }

            _availableByEntryId.Remove(entry.EntryId);
            loadout[entry.SlotId] = entry.PartId;
            PickIndex++;

            PickApplied?.Invoke(pick);
            if (IsComplete)
            {
                Completed?.Invoke();
            }

            return ArenaDraftPickResult.Applied;
        }

        /// <summary>
        /// The deterministic legal fill for a seat (timeout / AI / departed): the lowest
        /// (loadout-slot order, EntryId) available entry whose slot the player still needs.
        /// Board viability (P4-5 req 6) guarantees one exists while the draft is incomplete.
        /// </summary>
        public int AutoPickEntryFor(int playerId)
        {
            var loadout = LoadoutOf(playerId);
            for (int slotIndex = 0; slotIndex < _slotLoadout.Count; slotIndex++)
            {
                string slotId = _slotLoadout[slotIndex];
                if (loadout.ContainsKey(slotId))
                {
                    continue;
                }

                var entry = AvailableEntries
                    .Where(e => e.SlotId == slotId)
                    .OrderBy(e => e.EntryId)
                    .FirstOrDefault();
                if (entry != null)
                {
                    return entry.EntryId;
                }
            }

            throw new InvalidOperationException(
                $"No legal auto-pick for player {playerId}: the board no longer covers their open slots.");
        }
    }
}
