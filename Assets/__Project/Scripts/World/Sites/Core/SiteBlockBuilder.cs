using System.Collections.Generic;
using Narrative.Director.Core;

namespace World.Sites.Core
{
    /// <summary>
    /// Builds one site instance's block of platform slots from its capacity recipe: rolls the
    /// footprint, emits the anchor beat(s) first (the site's reason to exist — their eligibility was
    /// verified when the block was triggered), rolls the fill budget and draws each fill beat from the
    /// weighted table, and leaves the remainder as connective Empty (never dead space — the "breath"
    /// the dressing pass later fills). All rolls come from the caller's stream in a fixed order, so a
    /// block is deterministic per (seed, instance).
    /// </summary>
    public sealed class SiteBlockBuilder
    {
        public IReadOnlyList<SiteSlot> Build(SiteDefinitionData site, IRandomSource random, int instanceId)
        {
            int footprint = site.FootprintMin + random.NextInt(site.FootprintMax - site.FootprintMin + 1);

            var beats = new List<ContentBeat>(footprint);
            for (int i = 0; i < site.AnchorBeats.Count && beats.Count < footprint; i++)
            {
                beats.Add(site.AnchorBeats[i]);
            }

            int fillCap = footprint - beats.Count;
            int fillBudget = site.FillBudgetMin + random.NextInt(site.FillBudgetMax - site.FillBudgetMin + 1);
            if (fillBudget > fillCap)
            {
                fillBudget = fillCap;
            }

            for (int i = 0; i < fillBudget; i++)
            {
                if (!TryDrawFill(site.FillTable, random, out var beat))
                {
                    break;
                }

                beats.Add(beat);
            }

            while (beats.Count < footprint)
            {
                beats.Add(new ContentBeat(ContentBaseKind.Empty));
            }

            var slots = new SiteSlot[footprint];
            for (int i = 0; i < footprint; i++)
            {
                slots[i] = new SiteSlot(beats[i],
                    new SiteStamp(site.SiteId, instanceId, i, footprint, site.DressingThemeId));
            }

            return slots;
        }

        private static bool TryDrawFill(IReadOnlyList<WeightedBeat> table, IRandomSource random, out ContentBeat beat)
        {
            int total = 0;
            for (int i = 0; i < table.Count; i++)
            {
                total += table[i].Weight;
            }

            if (total <= 0)
            {
                // An empty/zero-weight fill table is an authored "anchor only" site, not an error.
                beat = default;
                return false;
            }

            int roll = random.NextInt(total);
            for (int i = 0; i < table.Count; i++)
            {
                roll -= table[i].Weight;
                if (roll < 0)
                {
                    beat = table[i].Beat;
                    return true;
                }
            }

            beat = table[table.Count - 1].Beat;
            return true;
        }
    }
}
