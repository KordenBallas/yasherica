using System;
using System.Collections.Generic;

namespace Combat.Arena.Core
{
    /// <summary>
    /// Host → all: one canonically applied pick. Every replica (the host's own included)
    /// advances its draft model only on these — the single state-advance path that makes the
    /// draft lockstep by construction. Departures since the previous broadcast piggyback here
    /// so all clients agree on which seats fold into round 1 dead.
    /// </summary>
    public class ArenaDraftPickApplied
    {
        public ArenaDraftPick Pick { get; }
        public IReadOnlyList<int> DepartedPlayerIds { get; }

        public ArenaDraftPickApplied(ArenaDraftPick pick, IReadOnlyList<int> departedPlayerIds)
        {
            Pick = pick;
            DepartedPlayerIds = departedPlayerIds ?? Array.Empty<int>();
        }
    }
}
