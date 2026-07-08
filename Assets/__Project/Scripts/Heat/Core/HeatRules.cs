namespace Heat.Core
{
    /// <summary>
    /// The composed rule changes a sealed pact demands (heat-ascension FR9): one immutable lens the
    /// installers project onto the per-system rule records, so no consuming system ever reads the
    /// pact or the menu itself. Magnitudes of every taken modifier sum per <see cref="HeatEffectKind"/> —
    /// composition is a plain deterministic sum, reproducible from (settings, pact) alone.
    /// </summary>
    public sealed class HeatRules
    {
        /// <summary>Heat 0 — no rule changes; today's game.</summary>
        public static readonly HeatRules Neutral = new HeatRules(0, false, 0, 0, 0);

        public int TotalHeat { get; }
        public bool EnemiesAlwaysLead { get; }
        public int EscalationTierLift { get; }
        public int VariantOptionCut { get; }
        public int SocketCut { get; }

        private HeatRules(int totalHeat, bool enemiesAlwaysLead, int escalationTierLift, int variantOptionCut, int socketCut)
        {
            TotalHeat = totalHeat;
            EnemiesAlwaysLead = enemiesAlwaysLead;
            EscalationTierLift = escalationTierLift;
            VariantOptionCut = variantOptionCut;
            SocketCut = socketCut;
        }

        public static HeatRules From(HeatSettings settings, HeatPact pact)
        {
            if (settings == null || pact == null || pact.TotalHeat == 0)
            {
                return Neutral;
            }

            bool enemiesLead = false;
            int tierLift = 0;
            int variantCut = 0;
            int socketCut = 0;
            for (int i = 0; i < pact.Ranks.Count; i++)
            {
                var entry = pact.Ranks[i];
                if (!settings.TryGetModifier(entry.ModifierId, out var modifier))
                {
                    continue;
                }

                int magnitude = modifier.MagnitudeAtRank(entry.Rank);
                switch (modifier.Kind)
                {
                    case HeatEffectKind.EnemiesActFirst:
                        enemiesLead |= magnitude > 0;
                        break;
                    case HeatEffectKind.RaisedCreatureFloor:
                        tierLift += magnitude;
                        break;
                    case HeatEffectKind.StingyCauldron:
                        variantCut += magnitude;
                        break;
                    case HeatEffectKind.FewerSockets:
                        socketCut += magnitude;
                        break;
                }
            }

            return new HeatRules(pact.TotalHeat, enemiesLead, tierLift, variantCut, socketCut);
        }
    }
}
