using System;
using System.Collections.Generic;
using Combat.Battlefield;
using LevelGeneration;
using Narrative.Director.Core;

namespace World.Dressing.Core
{
    /// <summary>
    /// Plans a platform's biome decoration deterministically (biome-decoration brief FR5–FR9):
    /// sparse whole-cell blocking obstacles that never break the battlefield minimum, the movement
    /// lane, or cell connectivity, plus homogeneous decorative clusters (a copse, an outcrop, a
    /// tuft patch — several small props may share a cell) biased to the platform's rim and rear
    /// (+Z is away from the camera). Pure C#; all draws come from the caller's seeded stream.
    /// </summary>
    public sealed class BiomeFeaturePlanner
    {
        /// <summary>Blockers stay "few and deliberate": at least this many cells apart.</summary>
        private const int BlockerMinSpacing = 2;

        /// <summary>Decorative cluster size range (a grouping reads from 2; 5 stays sparse).</summary>
        private const int ClusterSizeMin = 2;
        private const int ClusterSizeMax = 5;

        /// <summary>Cluster members scatter within this many cell radii of the cluster seed.</summary>
        private const float ClusterRadiusCells = 1.2f;

        /// <summary>Every Nth overhang-flagged large cluster anchors on the decorative rim.</summary>
        private const int RimAnchorEvery = 3;

        /// <summary>Rim-anchored members jitter tighter, so a leaning prop keeps its foothold on
        /// the platform edge — a lean, not a launch (decoration-footprint brief FR6).</summary>
        private const float OverhangJitterCells = 0.25f;

        /// <summary>How far from the walkable outline toward the rim ring the overhang anchor
        /// sits. Kept near the outline: the walkable top is at local Y=0 while the rim droops —
        /// a mid-rim anchor would float above the slope (and its jitter could leave the rim).</summary>
        private const float OverhangRimLean = 0.15f;

        /// <summary>Bias split between rearness and edge distance when scoring blocker cells.</summary>
        private const float RearScoreWeight = 0.6f;
        private const float EdgeScoreWeight = 0.4f;
        private const float ScoreJitter = 0.35f;

        public PlatformDressingPlan Plan(
            PlatformHexSurface surface,
            FeaturePoolData pool,
            FeatureDensitySettings density,
            LevelTheme theme,
            int battlefieldMinCells,
            IRandomSource rng)
        {
            if (surface == null || pool == null || pool.IsEmpty || rng == null)
            {
                return PlatformDressingPlan.Empty;
            }

            density ??= FeatureDensitySettings.CreateDefault();

            float laneHalfWidth = density.LaneHalfWidth * surface.HexSize;
            var protectedSet = ProtectedCells.Build(surface, laneHalfWidth);
            var placements = new List<DressingPlacement>();

            var blocked = PlanBlockers(surface, pool, density, battlefieldMinCells, protectedSet, rng, placements);
            PlanDecorativeClusters(surface, pool, density, protectedSet, rng, placements);

            // A bound kit always yields a kit-carrying plan, even with zero placements: the
            // biome ground material must dress featureless platforms too.
            return new PlatformDressingPlan(
                DressingPlanKind.BiomeFeatures, pool.KitId, theme, blocked, placements);
        }

        private static List<HexCoordinates> PlanBlockers(
            PlatformHexSurface surface,
            FeaturePoolData pool,
            FeatureDensitySettings density,
            int battlefieldMinCells,
            HashSet<HexCoordinates> protectedSet,
            IRandomSource rng,
            List<DressingPlacement> placements)
        {
            var blocked = new List<HexCoordinates>();
            var blockingEntries = CollectEntryIndices(pool, FeatureKind.Blocking);
            if (blockingEntries.Count == 0)
            {
                return blocked;
            }

            int cellCount = surface.Cells.Count;
            int target = (int)Math.Round(cellCount * density.BlockersPer100Cells / 100f);
            // The battlefield minimum is a floor, not a target (brief FR6): blocking may never
            // push the free cell count under it.
            target = Math.Min(target, BlockedCellGuard.MaxBlockable(surface, battlefieldMinCells, protectedSet.Count));
            if (target <= 0)
            {
                return blocked;
            }

            var candidates = ScoreAndSortCandidates(surface, protectedSet, rng);
            var blockedSet = new HashSet<HexCoordinates>();
            foreach (var cell in candidates)
            {
                if (blocked.Count >= target)
                {
                    break;
                }

                if (BlockedCellGuard.TooClose(cell, blocked, BlockerMinSpacing))
                {
                    continue;
                }

                blockedSet.Add(cell);
                if (!BlockedCellGuard.StaysConnected(surface, blockedSet))
                {
                    blockedSet.Remove(cell);
                    continue;
                }

                int entryIndex = DrawWeighted(pool, blockingEntries, rng);
                var entry = pool.Entries[entryIndex];
                float yaw = NextFloat(rng) * 360f;
                float scale = DrawScale(entry, rng);
                var (x, z) = surface.GetCellCenterLocal(cell);

                // An obstacle stays on its cell centre (it IS the cell), so it cannot be nudged:
                // a footprint that doesn't clear the silhouette skips this cell (brief FR1/FR2).
                if (PlatformEdgeFit.SignedClearance(surface.Outline, x, z) < entry.FootprintRadius * scale)
                {
                    blockedSet.Remove(cell);
                    continue;
                }

                blocked.Add(cell);
                placements.Add(new DressingPlacement(
                    DressingRole.Feature, entryIndex, x, z, yaw, scale));
            }

            return blocked;
        }

        private static void PlanDecorativeClusters(
            PlatformHexSurface surface,
            FeaturePoolData pool,
            FeatureDensitySettings density,
            HashSet<HexCoordinates> protectedSet,
            IRandomSource rng,
            List<DressingPlacement> placements)
        {
            var decorativeEntries = new List<int>();
            decorativeEntries.AddRange(CollectEntryIndices(pool, FeatureKind.SmallDecorative));
            decorativeEntries.AddRange(CollectEntryIndices(pool, FeatureKind.LargeDecorative));
            if (decorativeEntries.Count == 0)
            {
                return;
            }

            decorativeEntries.Sort();

            int clusterCount = (int)Math.Round(surface.Cells.Count * density.DecorClustersPer100Cells / 100f);
            var rearSorted = BuildRearSortedCells(surface, protectedSet);
            int largeClusterOrdinal = 0;

            for (int i = 0; i < clusterCount; i++)
            {
                int entryIndex = DrawWeighted(pool, decorativeEntries, rng);
                var entry = pool.Entries[entryIndex];
                bool isLarge = entry.Kind == FeatureKind.LargeDecorative;

                // The rim-framing overhang is an opt-in style (decoration-footprint brief FR5):
                // only a flagged prop may anchor on the rim and lean past the walkable edge.
                bool overhangCluster = false;
                float seedX;
                float seedZ;
                if (isLarge && entry.MayOverhang && ++largeClusterOrdinal % RimAnchorEvery == 0
                    && TryPickRimAnchor(surface, rng, out seedX, out seedZ))
                {
                    overhangCluster = true;
                }
                else if (!TryPickClusterSeed(surface, protectedSet, rearSorted, isLarge, rng, out seedX, out seedZ))
                {
                    continue;
                }

                int members = ClusterSizeMin + rng.NextInt(ClusterSizeMax - ClusterSizeMin + 1);
                float maxRadius = (overhangCluster ? OverhangJitterCells : ClusterRadiusCells)
                    * surface.HexSize;
                for (int m = 0; m < members; m++)
                {
                    float angle = NextFloat(rng) * 2f * (float)Math.PI;
                    float radius = NextFloat(rng) * maxRadius;
                    float memberX = seedX + radius * (float)Math.Cos(angle);
                    float memberZ = seedZ + radius * (float)Math.Sin(angle);
                    float yaw = NextFloat(rng) * 360f;
                    float scale = DrawScale(entry, rng);

                    // Grounded props must fit fully within the silhouette: nudge inward by the
                    // footprint, skip only when the prop genuinely cannot fit (brief FR1/FR3/FR4).
                    if (!overhangCluster && !PlatformEdgeFit.TryFitInside(
                            surface.Outline, memberX, memberZ, entry.FootprintRadius * scale,
                            out memberX, out memberZ))
                    {
                        continue;
                    }

                    placements.Add(new DressingPlacement(
                        DressingRole.Feature, entryIndex, memberX, memberZ, yaw, scale));
                }
            }
        }

        private static bool TryPickClusterSeed(
            PlatformHexSurface surface,
            HashSet<HexCoordinates> protectedSet,
            IReadOnlyList<HexCoordinates> rearSorted,
            bool isLarge,
            IRandomSource rng,
            out float x,
            out float z)
        {
            x = 0f;
            z = 0f;
            if (isLarge)
            {
                // Large clusters keep out of the lane and gravitate to the rear/edge: a squared
                // draw over the rear-sorted candidates biases toward the top (rear-most) cells.
                if (rearSorted.Count == 0)
                {
                    return false;
                }

                float u = NextFloat(rng);
                int index = (int)(u * u * rearSorted.Count);
                index = Math.Min(index, rearSorted.Count - 1);
                (x, z) = surface.GetCellCenterLocal(rearSorted[index]);
                return true;
            }

            // Small decoration may sit anywhere except the very center — it never blocks, so the
            // lane stays playable while looking dressed (decor and obstacle density decoupled).
            var cells = surface.Cells;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                var cell = cells[rng.NextInt(cells.Count)];
                if (!cell.Equals(surface.CenterCell))
                {
                    (x, z) = surface.GetCellCenterLocal(cell);
                    return true;
                }
            }

            return false;
        }

        private static bool TryPickRimAnchor(
            PlatformHexSurface surface, IRandomSource rng, out float x, out float z)
        {
            x = 0f;
            z = 0f;
            var outline = surface.SubdividedOutline;
            var rim = surface.RimRing;
            if (outline.Count == 0)
            {
                return false;
            }

            // Prefer rear-side (+Z) rim points; fall back to any.
            var rearIndices = new List<int>();
            for (int i = 0; i < outline.Count; i++)
            {
                if (outline[i].Z >= 0f)
                {
                    rearIndices.Add(i);
                }
            }

            int index = rearIndices.Count > 0
                ? rearIndices[rng.NextInt(rearIndices.Count)]
                : rng.NextInt(outline.Count);
            x = outline[index].X + (rim[index].X - outline[index].X) * OverhangRimLean;
            z = outline[index].Z + (rim[index].Z - outline[index].Z) * OverhangRimLean;
            return true;
        }

        private static List<HexCoordinates> ScoreAndSortCandidates(
            PlatformHexSurface surface, HashSet<HexCoordinates> protectedSet, IRandomSource rng)
        {
            GetBounds(surface, out float minZ, out float maxZ, out float maxRadial);
            float zRange = Math.Max(maxZ - minZ, 0.001f);
            float radialRange = Math.Max(maxRadial, 0.001f);

            var scored = new List<(HexCoordinates Cell, float Score)>();
            foreach (var cell in surface.Cells)
            {
                if (protectedSet.Contains(cell))
                {
                    continue;
                }

                var (x, z) = surface.GetCellCenterLocal(cell);
                float rear = (z - minZ) / zRange;
                float edge = (float)Math.Sqrt(x * x + z * z) / radialRange;
                float score = rear * RearScoreWeight + edge * EdgeScoreWeight + NextFloat(rng) * ScoreJitter;
                scored.Add((cell, score));
            }

            // Cells iterate in the surface's sorted order, so the jitter draws are deterministic;
            // the (Q,R) tie-break keeps equal scores stable.
            scored.Sort((a, b) =>
            {
                int byScore = b.Score.CompareTo(a.Score);
                if (byScore != 0)
                {
                    return byScore;
                }

                int byQ = a.Cell.Q.CompareTo(b.Cell.Q);
                return byQ != 0 ? byQ : a.Cell.R.CompareTo(b.Cell.R);
            });

            var result = new List<HexCoordinates>(scored.Count);
            foreach (var (cell, _) in scored)
            {
                result.Add(cell);
            }

            return result;
        }

        private static List<HexCoordinates> BuildRearSortedCells(
            PlatformHexSurface surface, HashSet<HexCoordinates> protectedSet)
        {
            var cells = new List<HexCoordinates>();
            foreach (var cell in surface.Cells)
            {
                if (!protectedSet.Contains(cell))
                {
                    cells.Add(cell);
                }
            }

            cells.Sort((a, b) =>
            {
                float za = surface.GetCellCenterLocal(a).Z;
                float zb = surface.GetCellCenterLocal(b).Z;
                int byZ = zb.CompareTo(za);
                if (byZ != 0)
                {
                    return byZ;
                }

                int byQ = a.Q.CompareTo(b.Q);
                return byQ != 0 ? byQ : a.R.CompareTo(b.R);
            });

            return cells;
        }

        private static void GetBounds(
            PlatformHexSurface surface, out float minZ, out float maxZ, out float maxRadial)
        {
            minZ = float.MaxValue;
            maxZ = float.MinValue;
            maxRadial = 0f;
            foreach (var cell in surface.Cells)
            {
                var (x, z) = surface.GetCellCenterLocal(cell);
                minZ = Math.Min(minZ, z);
                maxZ = Math.Max(maxZ, z);
                maxRadial = Math.Max(maxRadial, (float)Math.Sqrt(x * x + z * z));
            }
        }

        private static List<int> CollectEntryIndices(FeaturePoolData pool, FeatureKind kind)
        {
            var indices = new List<int>();
            for (int i = 0; i < pool.Entries.Count; i++)
            {
                if (pool.Entries[i].Kind == kind && pool.Entries[i].Weight > 0)
                {
                    indices.Add(i);
                }
            }

            return indices;
        }

        private static int DrawWeighted(FeaturePoolData pool, List<int> entryIndices, IRandomSource rng)
        {
            int total = 0;
            foreach (int index in entryIndices)
            {
                total += pool.Entries[index].Weight;
            }

            int roll = rng.NextInt(total);
            foreach (int index in entryIndices)
            {
                roll -= pool.Entries[index].Weight;
                if (roll < 0)
                {
                    return index;
                }
            }

            return entryIndices[entryIndices.Count - 1];
        }

        private static float DrawScale(FeatureEntryData entry, IRandomSource rng)
        {
            return entry.ScaleMin + (entry.ScaleMax - entry.ScaleMin) * NextFloat(rng);
        }

        private static float NextFloat(IRandomSource rng) => rng.NextInt(10000) / 10000f;
    }
}
