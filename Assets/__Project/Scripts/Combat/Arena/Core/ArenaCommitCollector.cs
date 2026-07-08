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
        private readonly HashSet<int> _passedPlayerIds = new HashSet<int>();
        private readonly Dictionary<int, ArenaCommit> _commits = new Dictionary<int, ArenaCommit>();
        private int _roundNumber;

        public int RoundNumber => _roundNumber;

        public bool AllCommitted => _requiredPlayerIds.Count > 0 && _commits.Count == _requiredPlayerIds.Count;

        public void BeginRound(int roundNumber, IReadOnlyList<int> requiredPlayerIds)
        {
            _roundNumber = roundNumber;
            _requiredPlayerIds.Clear();
            _passedPlayerIds.Clear();
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

        /// <summary>
        /// The authority passes a player's round for it (disconnect grace, X2 substitution): the
        /// seat stops owing a commit but — unlike a departure — stays alive and rides the bundle's
        /// auto-passed list so every client can present it. An already-accepted commit is dropped.
        /// </summary>
        public void MarkPassed(int playerId)
        {
            if (!_requiredPlayerIds.Remove(playerId))
                return;

            _commits.Remove(playerId);
            _passedPlayerIds.Add(playerId);
        }

        /// <summary>
        /// A rejoiner returned while its round is still open: it owes a commit again (the
        /// auto-pass is undone).
        /// </summary>
        public void Reinstate(int playerId)
        {
            if (!_passedPlayerIds.Remove(playerId))
                return;

            _requiredPlayerIds.Add(playerId);
        }

        /// <summary>Players the authority passed this round, canonical (ascending) order.</summary>
        public IReadOnlyList<int> PassedPlayerIds()
        {
            return _passedPlayerIds.OrderBy(id => id).ToList();
        }

        /// <summary>Commits in canonical order (ascending PlayerId) — the bundle's wire order.</summary>
        public IReadOnlyList<ArenaCommit> CommitsInCanonicalOrder()
        {
            return _commits.Values.OrderBy(c => c.PlayerId).ToList();
        }
    }
}
