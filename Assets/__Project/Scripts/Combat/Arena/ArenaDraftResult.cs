using System;
using System.Collections.Generic;

namespace Combat.Arena
{
    /// <summary>
    /// What the completed draft hands to the match build: each seat's slot → part-id loadout
    /// plus the seats that departed mid-draft (they still spawn — deterministically identical
    /// on every client — and fold into round 1 dead).
    /// </summary>
    public class ArenaDraftResult
    {
        public IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> LoadoutByPlayerId { get; }
        public IReadOnlyList<int> DepartedPlayerIds { get; }

        public ArenaDraftResult(
            IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> loadoutByPlayerId,
            IReadOnlyList<int> departedPlayerIds)
        {
            LoadoutByPlayerId = loadoutByPlayerId
                ?? new Dictionary<int, IReadOnlyDictionary<string, string>>();
            DepartedPlayerIds = departedPlayerIds ?? Array.Empty<int>();
        }
    }
}
