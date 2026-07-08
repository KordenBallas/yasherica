using System;

namespace Core.Persistence
{
    /// <summary>
    /// One taken Heat modifier in a persisted pact (heat-ascension FR4/FR12): the modifier's stable
    /// id at its taken rank. The total Heat is deliberately NOT persisted — it is recomputed from
    /// the authored menu on load, so a re-valued menu can never leave a stale total in a save.
    /// </summary>
    [Serializable]
    public class HeatPactEntryDto
    {
        public string ModifierId = string.Empty;
        public int Rank;
    }
}
