using System.Collections.Generic;
using System.Linq;
using Combat.Arena.Core;
using Core.Logging;

namespace Combat.Arena
{
    /// <summary>
    /// The hosting machine's round assembler: opens the round with the players that owe a commit,
    /// accepts commits off the transport, and broadcasts the canonical bundle the moment everyone
    /// alive has locked in. Pure relay authority — it never simulates; every client (the host
    /// included) resolves the identical bundle in its own deterministic sim. Also the R10 check
    /// point: commit envelopes piggyback the previous round's state hash, compared to the host's own.
    /// Disconnects ride the seat ledger (X1): a dropped seat is auto-passed while its grace lasts
    /// (its unit stays alive in place) and only folds into the departed list when grace expires.
    /// </summary>
    public class ArenaMatchHost
    {
        private readonly ArenaCommitCollector _collector;
        private readonly ArenaSeatLedger _seatLedger;
        private readonly IArenaTransport _transport;
        private readonly IGameLogger _logger;
        private readonly List<int> _pendingDeparted = new List<int>();
        private readonly List<int> _seededDeparted = new List<int>();

        private ulong _expectedPreviousHash;
        private bool _active;
        private bool _roundBroadcast;
        private bool _seatsSeeded;

        // X2 anti-cheat (armed via EnableValidation; inert otherwise so the relay-only MVP
        // behavior — and every test built on it — stays intact).
        private ArenaCommitValidator _validator;
        private ArenaQueueCreditLedger _queueCredits;
        private IArenaCanonicalStateSource _canonicalState;
        private int _maxQueueSize;

        /// <summary>Raised when a client's reported state hash disagrees with the host's (R10).</summary>
        public event System.Action<int> DesyncDetected;

        /// <summary>Raised when a relayed commit failed validation and was substituted with a pass (X2).</summary>
        public event System.Action<int, string> CommitRejected;

        public ArenaMatchHost(
            ArenaCommitCollector collector,
            ArenaSeatLedger seatLedger,
            IArenaTransport transport,
            IGameLogger logger)
        {
            _collector = collector;
            _seatLedger = seatLedger;
            _transport = transport;
            _logger = logger;
        }

        /// <summary>
        /// Arms host-side commit validation (X2): every relayed commit is re-validated against
        /// the canonical round-start state; an invalid one is replaced with a pass (no kick —
        /// safe under a false positive) and loudly logged. Called on every client at match build
        /// (validation only runs on whichever machine is the active host — including one
        /// promoted by migration).
        /// </summary>
        public void EnableValidation(
            ArenaCommitValidator validator,
            ArenaQueueCreditLedger queueCredits,
            IArenaCanonicalStateSource canonicalState,
            int maxQueueSize)
        {
            _validator = validator;
            _queueCredits = queueCredits;
            _canonicalState = canonicalState;
            _maxQueueSize = maxQueueSize;
        }

        /// <summary>Only the hosting machine activates; joined clients never assemble rounds.</summary>
        public void Activate()
        {
            if (_active)
                return;

            _active = true;
            _transport.CommitReceived += HandleCommit;
            _transport.PlayerDeparted += HandleDeparted;
        }

        public void Deactivate()
        {
            if (!_active)
                return;

            _active = false;
            _transport.CommitReceived -= HandleCommit;
            _transport.PlayerDeparted -= HandleDeparted;
        }

        /// <summary>
        /// Opens the seat book for the match (host only). A promoted migration host re-seeds with
        /// every unreachable seat pre-marked disconnected so they ride grace from round one.
        /// </summary>
        public void SeedSeats(
            int matchSeed,
            IReadOnlyList<ArenaRosterSlot> roster,
            int graceRounds,
            IReadOnlyCollection<int> disconnectedPlayerIds = null)
        {
            _seatLedger.Seed(matchSeed, roster, graceRounds, disconnectedPlayerIds);
            _seatsSeeded = true;
        }

        public void BeginRound(int roundNumber, IReadOnlyList<int> alivePlayerIds)
        {
            _roundBroadcast = false;
            _collector.BeginRound(roundNumber, alivePlayerIds);

            // Grace bookkeeping: expired seats become this round's departures; seats still riding
            // grace (or mid-resync) are auto-passed — alive, but owing no commit.
            if (_seatsSeeded)
            {
                foreach (var playerId in _seatLedger.TickRound())
                {
                    _pendingDeparted.Add(playerId);
                    _collector.RemovePlayer(playerId);
                }

                foreach (var playerId in _seatLedger.AbsentPlayerIds())
                {
                    _collector.MarkPassed(playerId);
                }
            }

            // Seats that vanished during the pre-fight draft fold into this round's departures:
            // they never commit, and every client kills their unit off the bundle.
            if (_seededDeparted.Count > 0)
            {
                foreach (var playerId in _seededDeparted)
                {
                    _pendingDeparted.Add(playerId);
                    _collector.RemovePlayer(playerId);
                    _seatLedger.MarkDeparted(playerId);
                }

                _seededDeparted.Clear();
            }
        }

        /// <summary>
        /// Pre-loads departures that happened before the round loop existed (mid-draft drops),
        /// so the first <see cref="BeginRound"/> folds them in like live departures.
        /// </summary>
        public void SeedDeparted(IReadOnlyList<int> playerIds)
        {
            if (playerIds != null)
            {
                _seededDeparted.AddRange(playerIds);
            }
        }

        /// <summary>The host sim's own end-of-round hash — the reference every client is compared to.</summary>
        public void SetExpectedHash(ulong hash)
        {
            _expectedPreviousHash = hash;
        }

        /// <summary>
        /// A rejoiner finished its state transfer: if its round is still open it owes a commit
        /// again (the auto-pass is undone); a round already broadcast simply picks it up next
        /// <see cref="BeginRound"/>.
        /// </summary>
        public void HandleReconnectAccepted(int playerId)
        {
            // The transferred state carries an empty queue — the seat's volley bank restarts.
            _queueCredits?.Reset(playerId);

            if (_roundBroadcast)
                return;

            _collector.Reinstate(playerId);
        }

        private void HandleCommit(ArenaCommitEnvelope envelope)
        {
            if (envelope?.Commit == null)
                return;

            if (envelope.PreviousRoundStateHash != 0
                && _expectedPreviousHash != 0
                && envelope.PreviousRoundStateHash != _expectedPreviousHash)
            {
                _logger.Error(LogCategory.Combat,
                    $"[ArenaMatchHost] LOCKSTEP DESYNC: player {envelope.Commit.PlayerId} reported hash " +
                    $"{envelope.PreviousRoundStateHash:X16}, host expected {_expectedPreviousHash:X16}");
                DesyncDetected?.Invoke(envelope.Commit.PlayerId);
            }

            if (!_collector.TryAccept(envelope.RoundNumber, envelope.Commit))
            {
                _logger.Warning(LogCategory.Combat,
                    $"[ArenaMatchHost] Rejected commit from player {envelope.Commit.PlayerId} " +
                    $"for round {envelope.RoundNumber} (stale, duplicate, or not required)");
                return;
            }

            // X2 anti-cheat: re-validate the accepted commit against the canonical round-start
            // state; failure substitutes a pass (MarkPassed drops the accepted commit) — the seat
            // wastes its round, the match never trusts the payload. The host's own short-circuited
            // commit runs the same path (a free self-check).
            if (_validator != null && _canonicalState?.RoundStartState != null)
            {
                var commit = envelope.Commit;
                var verdict = _validator.Validate(
                    _canonicalState.RoundStartState, commit, _maxQueueSize,
                    _queueCredits.CreditsOf(commit.PlayerId));
                if (!verdict.IsValid)
                {
                    _logger.Error(LogCategory.Combat,
                        $"[ArenaMatchHost] INVALID COMMIT from player {commit.PlayerId} " +
                        $"(round {envelope.RoundNumber}): {verdict.Reason} — substituted with a pass");
                    _collector.MarkPassed(commit.PlayerId);
                    CommitRejected?.Invoke(commit.PlayerId, verdict.Reason);
                    BroadcastIfComplete();
                    return;
                }

                if (commit.Steps.Count == 0)
                {
                    _queueCredits.BankSchedulingRound(commit.PlayerId);
                }
                else if (commit.Steps.Any(s => s.IsAbility))
                {
                    _queueCredits.SpendBank(commit.PlayerId);
                }
            }

            BroadcastIfComplete();
        }

        private void HandleDeparted(int playerId)
        {
            _seatLedger.MarkDisconnected(playerId);

            if (_seatsSeeded && _seatLedger.ConnectionOf(playerId) == ArenaSeatConnection.Gracing)
            {
                // The seat rides its grace: this round (and the following grace rounds) it is
                // auto-passed; its unit stays alive in place until grace expires.
                _collector.MarkPassed(playerId);
            }
            else
            {
                // No seat book (defensive) or the seat was already gone — the pre-X1 semantic:
                // immediate departure, the unit dies off the next bundle.
                _pendingDeparted.Add(playerId);
                _collector.RemovePlayer(playerId);
            }

            // A departure or pass can be the last missing lock of the round.
            BroadcastIfComplete();
        }

        private void BroadcastIfComplete()
        {
            if (_roundBroadcast || !_collector.AllCommitted)
                return;

            _roundBroadcast = true;
            var bundle = new ArenaRoundBundle(
                _collector.RoundNumber,
                _collector.CommitsInCanonicalOrder(),
                new List<int>(_pendingDeparted),
                _collector.PassedPlayerIds());
            _pendingDeparted.Clear();

            _transport.BroadcastBundle(bundle);
        }
    }
}
