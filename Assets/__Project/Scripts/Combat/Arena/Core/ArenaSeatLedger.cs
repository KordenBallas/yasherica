using System.Collections.Generic;
using System.Linq;

namespace Combat.Arena.Core
{
    /// <summary>How a match seat is currently attached to the session.</summary>
    public enum ArenaSeatConnection
    {
        /// <summary>Live connection; commits expected every round.</summary>
        Connected,

        /// <summary>Connection gone; the seat is auto-passed while its grace rounds count down.</summary>
        Gracing,

        /// <summary>A rejoin was approved; the seat waits for the state transfer to complete.</summary>
        Resyncing,

        /// <summary>Grace expired (or the seat dropped pre-round-loop) — the unit is dead, no rejoin.</summary>
        Departed
    }

    /// <summary>
    /// The authority's book of seats (host-side; a promoted migration host seeds a fresh one):
    /// who is connected, who is riding out disconnect grace, and who is gone for good. Owns the
    /// grace countdown and the rejoin-token check; the match host consumes it to decide
    /// auto-pass vs departure each round.
    /// </summary>
    public class ArenaSeatLedger
    {
        private class Seat
        {
            public int PlayerId;
            public ulong ClientId;
            public ArenaSeatConnection Connection;
            public int GraceRoundsLeft;
        }

        private readonly Dictionary<int, Seat> _seats = new Dictionary<int, Seat>();
        private int _matchSeed;
        private int _graceRounds;

        /// <summary>
        /// (Re)builds the book from the roster. <paramref name="disconnectedPlayerIds"/> lets a
        /// promoted migration host seed every unreachable seat straight into grace.
        /// </summary>
        public void Seed(
            int matchSeed,
            IReadOnlyList<ArenaRosterSlot> roster,
            int graceRounds,
            IReadOnlyCollection<int> disconnectedPlayerIds = null)
        {
            _matchSeed = matchSeed;
            _graceRounds = graceRounds;
            _seats.Clear();

            foreach (var slot in roster)
            {
                bool disconnected = disconnectedPlayerIds != null
                    && disconnectedPlayerIds.Contains(slot.PlayerId);
                _seats[slot.PlayerId] = new Seat
                {
                    PlayerId = slot.PlayerId,
                    ClientId = slot.ClientId,
                    Connection = disconnected ? ArenaSeatConnection.Gracing : ArenaSeatConnection.Connected,
                    GraceRoundsLeft = graceRounds
                };
            }
        }

        public ArenaSeatConnection ConnectionOf(int playerId)
        {
            return _seats.TryGetValue(playerId, out var seat)
                ? seat.Connection
                : ArenaSeatConnection.Departed;
        }

        /// <summary>Player ids whose seats are gone for good.</summary>
        public IReadOnlyList<int> DepartedPlayerIds()
        {
            var departed = new List<int>();
            foreach (var seat in _seats.Values)
            {
                if (seat.Connection == ArenaSeatConnection.Departed)
                {
                    departed.Add(seat.PlayerId);
                }
            }

            departed.Sort();
            return departed;
        }

        /// <summary>Player ids currently riding grace or mid-resync (owe no commit, still alive).</summary>
        public IReadOnlyList<int> AbsentPlayerIds()
        {
            var absent = new List<int>();
            foreach (var seat in _seats.Values)
            {
                if (seat.Connection == ArenaSeatConnection.Gracing
                    || seat.Connection == ArenaSeatConnection.Resyncing)
                {
                    absent.Add(seat.PlayerId);
                }
            }

            absent.Sort();
            return absent;
        }

        /// <summary>A live connection vanished: the seat starts (or keeps) riding its grace.</summary>
        public void MarkDisconnected(int playerId)
        {
            if (!_seats.TryGetValue(playerId, out var seat)
                || seat.Connection == ArenaSeatConnection.Departed)
            {
                return;
            }

            seat.Connection = ArenaSeatConnection.Gracing;
            seat.GraceRoundsLeft = _graceRounds;
        }

        /// <summary>The seat is gone for good (mid-draft drop or an explicit foreclosure).</summary>
        public void MarkDeparted(int playerId)
        {
            if (_seats.TryGetValue(playerId, out var seat))
            {
                seat.Connection = ArenaSeatConnection.Departed;
            }
        }

        /// <summary>
        /// A new round opens: a gracing seat with grace left burns one round (it will be
        /// auto-passed); a seat whose grace is spent expires. Returns the players whose grace
        /// just ran out — the caller folds them into the round's departures. A seat is therefore
        /// auto-passed for exactly <c>graceRounds</c> round-opens before departing.
        /// </summary>
        public IReadOnlyList<int> TickRound()
        {
            var expired = new List<int>();
            foreach (var seat in _seats.Values)
            {
                if (seat.Connection != ArenaSeatConnection.Gracing)
                    continue;

                if (seat.GraceRoundsLeft <= 0)
                {
                    seat.Connection = ArenaSeatConnection.Departed;
                    expired.Add(seat.PlayerId);
                }
                else
                {
                    seat.GraceRoundsLeft--;
                }
            }

            expired.Sort();
            return expired;
        }

        /// <summary>
        /// A connection claims a gracing seat with its derived token. On success the seat holds
        /// in <see cref="ArenaSeatConnection.Resyncing"/> (still auto-passed) until the state
        /// transfer completes via <see cref="CompleteRejoin"/>.
        /// </summary>
        public bool TryBeginRejoin(ulong token, ulong newClientId, int playerId)
        {
            if (!_seats.TryGetValue(playerId, out var seat))
                return false;
            if (seat.Connection != ArenaSeatConnection.Gracing)
                return false;
            if (token != ArenaRejoinToken.For(_matchSeed, playerId))
                return false;

            seat.Connection = ArenaSeatConnection.Resyncing;
            seat.ClientId = newClientId;
            return true;
        }

        /// <summary>The rejoiner acknowledged the state transfer — the seat is live again.</summary>
        public bool CompleteRejoin(int playerId)
        {
            if (!_seats.TryGetValue(playerId, out var seat)
                || seat.Connection != ArenaSeatConnection.Resyncing)
            {
                return false;
            }

            seat.Connection = ArenaSeatConnection.Connected;
            seat.GraceRoundsLeft = _graceRounds;
            return true;
        }

        public bool TryGetClientId(int playerId, out ulong clientId)
        {
            if (_seats.TryGetValue(playerId, out var seat))
            {
                clientId = seat.ClientId;
                return true;
            }

            clientId = 0;
            return false;
        }
    }
}
