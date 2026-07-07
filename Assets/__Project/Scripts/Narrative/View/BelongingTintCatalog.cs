using System;
using System.Collections.Generic;
using Narrative.Quests.Data;
using UnityEngine;
using World.Races.Data;

namespace Narrative.View
{
    /// <summary>
    /// Default <see cref="IBelongingTintCatalog"/>: merges the authored race belonging colours
    /// (<see cref="RaceDefinition"/>) and reward-family colours (<see cref="RewardFamilyDefinition"/>)
    /// into one id → colour lookup, read straight off the SO assets since Core records deliberately
    /// hold no Unity colour. Race and family ids share one namespace by convention (races are
    /// 'ibex'/'lizard'/'fox', families 'power'/'utility'); a collision warns nowhere and race wins —
    /// keep the vocabularies disjoint when authoring.
    /// </summary>
    public class BelongingTintCatalog : IBelongingTintCatalog
    {
        private readonly Dictionary<string, Color> _tintById =
            new Dictionary<string, Color>(StringComparer.Ordinal);

        public BelongingTintCatalog(IReadOnlyList<RaceDefinition> races,
            IReadOnlyList<RewardFamilyDefinition> families)
        {
            if (races != null)
            {
                foreach (var race in races)
                {
                    if (race != null && !string.IsNullOrEmpty(race.RaceId)
                        && !_tintById.ContainsKey(race.RaceId))
                    {
                        _tintById.Add(race.RaceId, race.BelongingColor);
                    }
                }
            }

            if (families != null)
            {
                foreach (var family in families)
                {
                    if (family != null && !string.IsNullOrEmpty(family.FamilyId)
                        && !_tintById.ContainsKey(family.FamilyId))
                    {
                        _tintById.Add(family.FamilyId, family.BelongingColor);
                    }
                }
            }
        }

        public Color TintFor(string belongingId)
        {
            return !string.IsNullOrEmpty(belongingId) && _tintById.TryGetValue(belongingId, out var tint)
                ? tint
                : Color.white;
        }
    }
}
