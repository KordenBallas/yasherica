using System.Collections.Generic;
using Heat.Core;

namespace Heat.Data
{
    /// <summary>
    /// SO → Core bridge for the Heat tuning asset. A null SO (missing asset) maps to the inert Core
    /// defaults so the game always boots with Heat simply absent (FR13). Modifier rows without an id
    /// or without ranks are dropped — a half-authored row can degrade the menu, never break a run.
    /// </summary>
    public static class HeatConfigMapper
    {
        public static HeatSettings ToSettings(HeatConfig config)
        {
            if (config == null)
            {
                return HeatSettings.Defaults;
            }

            var modifiers = new List<HeatModifier>();
            var seenIds = new HashSet<string>();
            if (config.Modifiers != null)
            {
                foreach (var authoring in config.Modifiers)
                {
                    if (authoring == null
                        || string.IsNullOrEmpty(authoring.Id)
                        || authoring.Ranks == null
                        || authoring.Ranks.Count == 0
                        || !seenIds.Add(authoring.Id))
                    {
                        continue;
                    }

                    var ranks = new List<HeatRank>(authoring.Ranks.Count);
                    foreach (var rank in authoring.Ranks)
                    {
                        if (rank != null)
                        {
                            ranks.Add(new HeatRank(rank.HeatValue, rank.Magnitude, rank.Description));
                        }
                    }

                    if (ranks.Count > 0)
                    {
                        modifiers.Add(new HeatModifier(authoring.Id, authoring.DisplayName, authoring.Kind, ranks));
                    }
                }
            }

            return new HeatSettings(
                modifiers,
                config.FloorReliefRunsPerHeat,
                config.BiasStrengthLiftPerHeat,
                config.ReserveDirectionSlotMinHeat,
                config.SoftCapTotalHeat,
                config.ClearWindowFloor);
        }
    }
}
