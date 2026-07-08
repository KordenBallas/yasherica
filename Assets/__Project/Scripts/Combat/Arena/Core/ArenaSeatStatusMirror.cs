using System.Collections.Generic;
using System.Linq;

namespace Combat.Arena.Core
{
    /// <summary>A seat's liveness as any client can see it from the broadcast stream.</summary>
    public enum ArenaSeatLiveness
    {
        Connected,

        /// <summary>Auto-passed in the latest bundle — riding disconnect grace.</summary>
        Absent,

        Departed
    }

    /// <summary>
    /// Every client's view of seat liveness + reachability, fed purely by what the host already
    /// broadcasts (bundles carry departures and auto-passes; the address book carries claimed
    /// endpoints). This is what a surviving client consults when the HOST is the one that died:
    /// the election needs "who is still alive and reachable" without asking anyone.
    /// </summary>
    public class ArenaSeatStatusMirror
    {
        private readonly Dictionary<int, ArenaSeatLiveness> _liveness = new Dictionary<int, ArenaSeatLiveness>();
        private readonly Dictionary<int, string> _addresses = new Dictionary<int, string>();

        /// <summary>The seat currently assembling rounds (re-pointed after a migration).</summary>
        public int HostPlayerId { get; private set; }

        public void Reset(IReadOnlyList<ArenaRosterSlot> roster)
        {
            _liveness.Clear();
            _addresses.Clear();
            if (roster == null || roster.Count == 0)
            {
                HostPlayerId = 0;
                return;
            }

            foreach (var slot in roster)
            {
                _liveness[slot.PlayerId] = ArenaSeatLiveness.Connected;
            }

            // The hosting machine owns the lowest connection id (the NGO server convention).
            HostPlayerId = roster.OrderBy(s => s.ClientId).ThenBy(s => s.PlayerId).First().PlayerId;
        }

        public void SetHost(int playerId)
        {
            HostPlayerId = playerId;
        }

        public ArenaSeatLiveness LivenessOf(int playerId)
        {
            return _liveness.TryGetValue(playerId, out var liveness)
                ? liveness
                : ArenaSeatLiveness.Departed;
        }

        /// <summary>Folds one broadcast round into the view (departures are sticky).</summary>
        public void ApplyBundle(ArenaRoundBundle bundle)
        {
            foreach (var commit in bundle.Commits)
            {
                if (LivenessOf(commit.PlayerId) != ArenaSeatLiveness.Departed)
                {
                    _liveness[commit.PlayerId] = ArenaSeatLiveness.Connected;
                }
            }

            foreach (var playerId in bundle.AutoPassedPlayerIds)
            {
                if (LivenessOf(playerId) != ArenaSeatLiveness.Departed)
                {
                    _liveness[playerId] = ArenaSeatLiveness.Absent;
                }
            }

            foreach (var playerId in bundle.DepartedPlayerIds)
            {
                _liveness[playerId] = ArenaSeatLiveness.Departed;
            }
        }

        public void ApplyAddressBook(ArenaAddressBook book)
        {
            foreach (var endpoint in book.Endpoints)
            {
                if (!string.IsNullOrEmpty(endpoint.Address))
                {
                    _addresses[endpoint.PlayerId] = endpoint.Address;
                }
            }
        }

        public bool TryGetAddress(int playerId, out string address)
        {
            return _addresses.TryGetValue(playerId, out address);
        }

        /// <summary>
        /// Who may take over when <paramref name="lostHostPlayerId"/> drops: every seat still
        /// Connected by the latest broadcast, the lost host excluded. Ascending — deterministic
        /// on every survivor, which is the whole point.
        /// </summary>
        public IReadOnlyList<int> ElectionCandidates(int lostHostPlayerId)
        {
            return _liveness
                .Where(pair => pair.Value == ArenaSeatLiveness.Connected && pair.Key != lostHostPlayerId)
                .Select(pair => pair.Key)
                .OrderBy(id => id)
                .ToList();
        }
    }
}
