using System;
using System.Collections.Generic;
using Combat.Battlefield;
using LevelGeneration;
using World.Sites.Core;

namespace World.Dressing.Core
{
    /// <summary>
    /// Plans one site-block platform's dressing deterministically (site-camp-dressing brief
    /// FR7–FR8): structures grouped to the rear along a Z-band shared by the whole block (the
    /// aligned-skyline read), a gate on the anchor platform's approach edge, and — for a kit with
    /// no architecture (the camp) — a focal piece with props gathered around it. The kit's shape
    /// decides the style (structures ⇒ settlement, focal ⇒ camp): data, not site-id switches.
    /// Pure C#; instance draws are shared block-wide, platform draws are per-platform.
    /// </summary>
    public sealed class SiteDressingPlanner
    {
        private const int StructureMinSpacing = 2;
        /// <summary>One structure per this many surface cells; every site platform gets at least one.</summary>
        private const int CellsPerStructure = 10;
        private const int MaxStructuresPerPlatform = 3;
        /// <summary>The shared skyline band starts this fraction of the way to the rear edge.</summary>
        private const float FrontLineFractionMin = 0.35f;
        private const float FrontLineFractionMax = 0.6f;
        private const float SharedScaleMin = 0.9f;
        private const float SharedScaleMax = 1.1f;
        /// <summary>Structures face the camera (−Z) so house fronts form the shared street line.</summary>
        private const float StructureYawDegrees = 180f;
        /// <summary>Gate pieces face the approaching hero (−X, the previous platform).</summary>
        private const float GateYawDegrees = 270f;
        private const float LaneHalfWidthCells = 1.1f;
        private const int PropsPerStructure = 2;
        private const int CampRingPropsMin = 4;
        private const int CampRingPropsMax = 6;
        private const float CampRingRadiusCellsMin = 1.0f;
        private const float CampRingRadiusCellsMax = 1.5f;
        /// <summary>The camp's fire sits toward the rear-center, off the movement lane.</summary>
        private const float FocalRearFraction = 0.6f;

        // Rough edge footprints for the site roles (the decoration-footprint rule extended here —
        // playtest showed camp rings and gates spilling past the silhouette): small props and gate
        // pieces are nudged inward like biome decoration; a structure/focal CELL prefers a centre
        // clearing the edge by more than a cell's inradius (~1.73 at hex size 2 — every cell centre
        // clears that much by construction), so wide houses and the fire's prop ring move one ring
        // inward. When no cell qualifies (small blobs) the margin falls back off — a slightly
        // overhanging house beats an undressed site, and ring members are edge-fitted regardless.
        private const float PropFootprint = 0.4f;
        private const float GateFootprint = 0.5f;
        private const float StructureEdgeMargin = 2.5f;
        private const float FocalEdgeMargin = 2.5f;

        public PlatformDressingPlan Plan(
            PlatformHexSurface surface,
            SiteStamp site,
            SiteKitData kit,
            LevelTheme theme,
            int battlefieldMinCells,
            Narrative.Director.Core.IRandomSource instanceRng,
            Narrative.Director.Core.IRandomSource platformRng)
        {
            if (surface == null || kit == null || kit.IsEmpty || site.IsWild
                || instanceRng == null || platformRng == null)
            {
                return PlatformDressingPlan.Empty;
            }

            // Block-shared draws, in fixed order — every platform of the instance re-derives the
            // same skyline band and structure scale, which is what aligns the rooflines.
            float frontLineFraction = FrontLineFractionMin
                + (FrontLineFractionMax - FrontLineFractionMin) * NextFloat(instanceRng);
            float sharedScale = SharedScaleMin + (SharedScaleMax - SharedScaleMin) * NextFloat(instanceRng);

            float laneHalfWidth = LaneHalfWidthCells * surface.HexSize;
            var protectedSet = ProtectedCells.Build(surface, laneHalfWidth);
            var placements = new List<DressingPlacement>();
            var blocked = new List<HexCoordinates>();

            if (kit.StructureCount > 0)
            {
                PlanStructures(
                    surface, kit, battlefieldMinCells, protectedSet, frontLineFraction, sharedScale,
                    platformRng, placements, blocked);
                PlanStructureProps(surface, kit, platformRng, placements);
            }
            else if (kit.FocalCount > 0)
            {
                PlanCampFocalAndRing(
                    surface, kit, battlefieldMinCells, protectedSet, sharedScale, platformRng,
                    placements, blocked);
            }

            if (site.Index == 0 && kit.GateCount > 0)
            {
                PlanGate(surface, kit, platformRng, placements);
            }

            // A resolved kit always yields a kit-carrying plan, even with zero placements: the
            // site's ground overlay must dress every block platform (shared ground = the
            // "one place" read), including one too small to hold a structure.
            return new PlatformDressingPlan(
                DressingPlanKind.SiteDressing, site.DressingThemeId, theme, blocked, placements);
        }

        private void PlanStructures(
            PlatformHexSurface surface,
            SiteKitData kit,
            int battlefieldMinCells,
            HashSet<HexCoordinates> protectedSet,
            float frontLineFraction,
            float sharedScale,
            Narrative.Director.Core.IRandomSource platformRng,
            List<DressingPlacement> placements,
            List<HexCoordinates> blocked)
        {
            GetZBounds(surface, out float minZ, out float maxZ);
            float frontZ = minZ + (maxZ - minZ) * frontLineFraction;

            // Candidates in the shared rear band, swept left-to-right so houses read as fronting
            // one line; the band (not per-cell jitter) is what the whole block shares.
            // A house is wider than its cell: prefer cells clear of the edge; when the blob is too
            // small to have any, fall back to the plain rear band (the lesser evil).
            var candidates = CollectStructureCandidates(
                surface, protectedSet, frontZ, StructureEdgeMargin);
            if (candidates.Count == 0)
            {
                candidates = CollectStructureCandidates(surface, protectedSet, frontZ, 0f);
            }

            candidates.Sort((a, b) =>
            {
                float xa = surface.GetCellCenterLocal(a).X;
                float xb = surface.GetCellCenterLocal(b).X;
                int byX = xa.CompareTo(xb);
                if (byX != 0)
                {
                    return byX;
                }

                // Prefer the rear-most cell at the same sweep position.
                float za = surface.GetCellCenterLocal(a).Z;
                float zb = surface.GetCellCenterLocal(b).Z;
                return zb.CompareTo(za);
            });

            int target = Math.Min(
                MaxStructuresPerPlatform, Math.Max(1, surface.Cells.Count / CellsPerStructure));
            target = Math.Min(
                target, BlockedCellGuard.MaxBlockable(surface, battlefieldMinCells, protectedSet.Count));

            var blockedSet = new HashSet<HexCoordinates>(blocked);
            foreach (var cell in candidates)
            {
                if (blocked.Count >= target)
                {
                    break;
                }

                if (BlockedCellGuard.TooClose(cell, blocked, StructureMinSpacing))
                {
                    continue;
                }

                blockedSet.Add(cell);
                if (!BlockedCellGuard.StaysConnected(surface, blockedSet))
                {
                    blockedSet.Remove(cell);
                    continue;
                }

                blocked.Add(cell);
                var (x, z) = surface.GetCellCenterLocal(cell);
                placements.Add(new DressingPlacement(
                    DressingRole.Structure, platformRng.NextInt(kit.StructureCount),
                    x, z, StructureYawDegrees, sharedScale));
            }
        }

        private static List<HexCoordinates> CollectStructureCandidates(
            PlatformHexSurface surface,
            HashSet<HexCoordinates> protectedSet,
            float frontZ,
            float edgeMargin)
        {
            var candidates = new List<HexCoordinates>();
            foreach (var cell in surface.Cells)
            {
                if (protectedSet.Contains(cell))
                {
                    continue;
                }

                var (cx, cz) = surface.GetCellCenterLocal(cell);
                if (cz < frontZ)
                {
                    continue;
                }

                if (edgeMargin > 0f
                    && PlatformEdgeFit.SignedClearance(surface.Outline, cx, cz) < edgeMargin)
                {
                    continue;
                }

                candidates.Add(cell);
            }

            return candidates;
        }

        private static void PlanStructureProps(
            PlatformHexSurface surface,
            SiteKitData kit,
            Narrative.Director.Core.IRandomSource platformRng,
            List<DressingPlacement> placements)
        {
            if (kit.PropCount == 0)
            {
                return;
            }

            // Street clutter in front of each placed house: props read as belonging to it.
            var structures = new List<DressingPlacement>();
            foreach (var placement in placements)
            {
                if (placement.Role == DressingRole.Structure)
                {
                    structures.Add(placement);
                }
            }

            foreach (var structure in structures)
            {
                for (int i = 0; i < PropsPerStructure; i++)
                {
                    float offsetX = (NextFloat(platformRng) - 0.5f) * 2.4f;
                    float offsetZ = -(0.8f + NextFloat(platformRng) * 0.8f);
                    int entryIndex = platformRng.NextInt(kit.PropCount);
                    float yaw = NextFloat(platformRng) * 360f;
                    float px = structure.LocalX + offsetX;
                    float pz = structure.LocalZ + offsetZ;
                    if (!PlatformEdgeFit.TryFitInside(surface.Outline, px, pz, PropFootprint, out px, out pz))
                    {
                        continue;
                    }

                    placements.Add(new DressingPlacement(
                        DressingRole.Prop, entryIndex, px, pz, yaw, 1f));
                }
            }
        }

        private static void PlanCampFocalAndRing(
            PlatformHexSurface surface,
            SiteKitData kit,
            int battlefieldMinCells,
            HashSet<HexCoordinates> protectedSet,
            float sharedScale,
            Narrative.Director.Core.IRandomSource platformRng,
            List<DressingPlacement> placements,
            List<HexCoordinates> blocked)
        {
            if (BlockedCellGuard.MaxBlockable(surface, battlefieldMinCells, protectedSet.Count) < 1)
            {
                return;
            }

            if (!TryFindFocalCell(surface, protectedSet, out var focalCell))
            {
                return;
            }

            blocked.Add(focalCell);
            var (fx, fz) = surface.GetCellCenterLocal(focalCell);

            // The composed fire read: every focal entry stacks on the one cell.
            for (int i = 0; i < kit.FocalCount; i++)
            {
                placements.Add(new DressingPlacement(
                    DressingRole.Focal, i, fx, fz, NextFloat(platformRng) * 360f, sharedScale));
            }

            if (kit.PropCount == 0)
            {
                return;
            }

            // Props gathered around the fire, each turned to face it (the "camped here" read).
            int ringCount = CampRingPropsMin
                + platformRng.NextInt(CampRingPropsMax - CampRingPropsMin + 1);
            float angleStep = 360f / ringCount;
            for (int i = 0; i < ringCount; i++)
            {
                float angleDegrees = i * angleStep + (NextFloat(platformRng) - 0.5f) * angleStep * 0.5f;
                double radians = angleDegrees * Math.PI / 180.0;
                float radius = (CampRingRadiusCellsMin
                    + NextFloat(platformRng) * (CampRingRadiusCellsMax - CampRingRadiusCellsMin))
                    * surface.HexSize;
                float px = fx + radius * (float)Math.Cos(radians);
                float pz = fz + radius * (float)Math.Sin(radians);
                int entryIndex = platformRng.NextInt(kit.PropCount);
                // A ring member near the platform edge is pulled back onto solid ground —
                // gathered around the fire, never hovering over the gap.
                if (!PlatformEdgeFit.TryFitInside(surface.Outline, px, pz, PropFootprint, out px, out pz))
                {
                    continue;
                }

                // Face the fire: yaw pointing from the prop back to the focal center.
                float yaw = (float)(Math.Atan2(fx - px, fz - pz) * 180.0 / Math.PI);
                placements.Add(new DressingPlacement(
                    DressingRole.Prop, entryIndex, px, pz, yaw, 1f));
            }
        }

        private static bool TryFindFocalCell(
            PlatformHexSurface surface, HashSet<HexCoordinates> protectedSet, out HexCoordinates focal)
        {
            // The fire needs room around it (the prop ring gathers at 1-1.5 cells): prefer a cell
            // clear of the edge; a blob too small to have one keeps its fire anyway (the ring
            // members are edge-fitted individually).
            return TryFindFocalCell(surface, protectedSet, FocalEdgeMargin, out focal)
                || TryFindFocalCell(surface, protectedSet, 0f, out focal);
        }

        private static bool TryFindFocalCell(
            PlatformHexSurface surface,
            HashSet<HexCoordinates> protectedSet,
            float edgeMargin,
            out HexCoordinates focal)
        {
            GetZBounds(surface, out float minZ, out float maxZ);
            float targetZ = minZ + (maxZ - minZ) * FocalRearFraction;

            focal = default;
            float bestSq = float.MaxValue;
            bool found = false;
            foreach (var cell in surface.Cells)
            {
                if (protectedSet.Contains(cell))
                {
                    continue;
                }

                var (x, z) = surface.GetCellCenterLocal(cell);
                if (edgeMargin > 0f
                    && PlatformEdgeFit.SignedClearance(surface.Outline, x, z) < edgeMargin)
                {
                    continue;
                }

                float dz = z - targetZ;
                float sq = x * x + dz * dz;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    focal = cell;
                    found = true;
                }
            }

            return found;
        }

        private static void PlanGate(
            PlatformHexSurface surface,
            SiteKitData kit,
            Narrative.Director.Core.IRandomSource platformRng,
            List<DressingPlacement> placements)
        {
            // The threshold read: gate pieces flank the movement lane on the approach (min-X) edge.
            float laneHalfWidth = LaneHalfWidthCells * surface.HexSize;
            float minX = float.MaxValue;
            foreach (var cell in surface.Cells)
            {
                var (x, z) = surface.GetCellCenterLocal(cell);
                if (z >= -laneHalfWidth && z <= laneHalfWidth)
                {
                    minX = Math.Min(minX, x);
                }
            }

            if (minX == float.MaxValue)
            {
                return;
            }

            int pieces = Math.Min(kit.GateCount, 2);
            for (int i = 0; i < pieces; i++)
            {
                // Two pieces straddle the lane; a single piece sits on its far side.
                float side = pieces == 2 && i == 0 ? -1f : 1f;
                float offsetZ = side * (laneHalfWidth + surface.HexSize * 0.5f);
                int entryIndex = platformRng.NextInt(kit.GateCount);
                // The straddle offset can leave the silhouette on a narrow approach edge —
                // pull the piece back onto the platform.
                if (!PlatformEdgeFit.TryFitInside(
                        surface.Outline, minX, offsetZ, GateFootprint, out float gx, out float gz))
                {
                    continue;
                }

                placements.Add(new DressingPlacement(
                    DressingRole.Gate, entryIndex, gx, gz, GateYawDegrees, 1f));
            }
        }

        private static void GetZBounds(PlatformHexSurface surface, out float minZ, out float maxZ)
        {
            minZ = float.MaxValue;
            maxZ = float.MinValue;
            foreach (var cell in surface.Cells)
            {
                float z = surface.GetCellCenterLocal(cell).Z;
                minZ = Math.Min(minZ, z);
                maxZ = Math.Max(maxZ, z);
            }
        }

        private static float NextFloat(Narrative.Director.Core.IRandomSource rng)
            => rng.NextInt(10000) / 10000f;
    }
}
