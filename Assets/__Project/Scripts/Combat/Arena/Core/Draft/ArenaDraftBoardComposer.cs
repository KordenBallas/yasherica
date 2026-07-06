using System;
using System.Collections.Generic;
using System.Linq;
using Loot.Core;

namespace Combat.Arena.Core
{
    /// <summary>
    /// Pure, deterministic board composition (P4-5 reqs 5–7): the ordinal-sorted common floor
    /// in full, plus a seeded sample of the participants' tasted-catalog union. Runs on the
    /// host only — clients receive the composed board over the wire and never recompute it.
    /// </summary>
    public static class ArenaDraftBoardComposer
    {
        private const string BoardSeedContext = "arena-draft-board";

        public static List<ArenaDraftBoardEntry> Compose(
            int matchSeed,
            IReadOnlyList<ArenaDraftPartInfo> floorParts,
            IReadOnlyList<ArenaDraftPartInfo> catalogUnion,
            IReadOnlyList<string> slotLoadout,
            int catalogSampleSize)
        {
            if (slotLoadout == null || slotLoadout.Count == 0)
            {
                throw new ArgumentException("Slot loadout must not be empty.", nameof(slotLoadout));
            }

            var loadoutSet = new HashSet<string>(slotLoadout, StringComparer.Ordinal);
            var board = new List<ArenaDraftBoardEntry>();
            int nextEntryId = 1;

            // The floor goes on the board in full, ordinal-ordered so entry ids are stable
            // regardless of authoring order in the config asset.
            var orderedFloor = (floorParts ?? Array.Empty<ArenaDraftPartInfo>())
                .Where(p => loadoutSet.Contains(p.SlotId))
                .OrderBy(p => p.SlotId, StringComparer.Ordinal)
                .ThenBy(p => p.PartId, StringComparer.Ordinal);
            var floorPartIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var part in orderedFloor)
            {
                board.Add(new ArenaDraftBoardEntry(nextEntryId++, part.PartId, part.SlotId));
                floorPartIds.Add(part.PartId);
            }

            // Catalog candidates: distinct ids not already stocked by the floor, ordinal-sorted
            // so the seeded sample is independent of input order.
            var candidates = (catalogUnion ?? Array.Empty<ArenaDraftPartInfo>())
                .Where(p => loadoutSet.Contains(p.SlotId) && !floorPartIds.Contains(p.PartId))
                .GroupBy(p => p.PartId, StringComparer.Ordinal)
                .Select(g => g.First())
                .OrderBy(p => p.PartId, StringComparer.Ordinal)
                .ToList();

            foreach (var picked in SamplePrefix(candidates, catalogSampleSize, matchSeed))
            {
                board.Add(new ArenaDraftBoardEntry(nextEntryId++, picked.PartId, picked.SlotId));
            }

            return board;
        }

        private static IEnumerable<ArenaDraftPartInfo> SamplePrefix(
            List<ArenaDraftPartInfo> candidates, int sampleSize, int matchSeed)
        {
            if (sampleSize >= candidates.Count)
            {
                return candidates;
            }

            // Fisher–Yates prefix over the sorted candidates with a context-derived seed.
            var random = new Random(LootSeed.Derive(matchSeed, BoardSeedContext));
            var pool = new List<ArenaDraftPartInfo>(candidates);
            var picked = new List<ArenaDraftPartInfo>(sampleSize);
            for (int i = 0; i < sampleSize; i++)
            {
                int index = random.Next(i, pool.Count);
                (pool[i], pool[index]) = (pool[index], pool[i]);
                picked.Add(pool[i]);
            }

            // Re-sort the sample so entry ids stay ordinal within the catalog block.
            return picked.OrderBy(p => p.PartId, StringComparer.Ordinal);
        }
    }
}
