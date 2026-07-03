using System.Collections.Generic;
using System.Linq;

namespace Combat.Arena.Core
{
    /// <summary>
    /// Host-side round gathering: tracks which alive players still owe a commit this round,
    /// accepts exactly one commit per player (duplicates and stale rounds are ignored), and
    /// reports completion so the host can assemble the bundle.
    /// </summary>
    public class ArenaCommitCollector
    {
        private readonly HashSet<int> _requiredPlayerIds = new HashSet<int>();
        private readonly Dictionary<int, ArenaCommit> _commits = new Dictionary<int, ArenaCommit>();
        private int _roundNumber;

        public int RoundNumber => _roundNumber;

        public bool AllCommitted => _requiredPlayerIds.Count > 0 && _commits.Count == _requiredPlayerIds.Count;

        public void BeginRound(int roundNumber, IReadOnlyList<int> requiredPlayerIds)
        {
            _roundNumber = roundNumber;
            _requiredPlayerIds.Clear();
            _commits.Clear();
            foreach (var id in requiredPlayerIds)
            {
                _requiredPlayerIds.Add(id);
            }
        }

        /// <summary>
        /// Accepts a commit for the current round. Returns false for stale rounds, players not
        /// required this round, or duplicate commits (first one wins).
        /// </summary>
        public bool TryAccept(int roundNumber, ArenaCommit commit)
        {
            if (roundNumber != _roundNumber || commit == null)
                return false;
            if (!_requiredPlayerIds.Contains(commit.PlayerId))
                return false;
            if (_commits.ContainsKey(commit.PlayerId))
                return false;

            _commits[commit.PlayerId] = commit;
            return true;
        }

        /// <summary>
        /// A player left mid-planning: it no longer owes a commit (its already-accepted commit,
        /// if any, is dropped — the departure rides the bundle's departed list instead).
        /// </summary>
        public void RemovePlayer(int playerId)
        {
            _requiredPlayerIds.Remove(playerId);
            _commits.Remove(playerId);
        }

        /// <summary>Commits in canonical order (ascending PlayerId) — the bundle's wire order.</summary>
        public IReadOnlyList<ArenaCommit> CommitsInCanonicalOrder()
        {
            return _commits.Values.OrderBy(c => c.PlayerId).ToList();
        }
    }
}
