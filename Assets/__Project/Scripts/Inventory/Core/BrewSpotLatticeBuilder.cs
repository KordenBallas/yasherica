using System;
using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// Builds the deterministic bottom-up spot lattice the brew layout assigns
    /// artifacts to (Track F stable spots). The diorama is viewed from the front,
    /// so a row is a horizontal spread along X (the screen axis), rows are
    /// stacked from the bowl floor toward the rim, and each row is clipped to
    /// the bowl's inner radius at its height. Rows share the same X grid, so
    /// spots align into **vertical columns** — the unit the gravity settle
    /// drops (FR4, 2026-07-07 revision: only the column above a removed bubble
    /// falls). Spot order IS the fill order: rows bottom-up, centre-out within
    /// a row, so the pile grows from the bottom and the fullness waterline can
    /// ride its top.
    /// </summary>
    public static class BrewSpotLatticeBuilder
    {
        // Plastic-number fraction spreads per-spot depth jitter evenly and
        // deterministically without Random state.
        private const float PlasticRatio = 0.7548776662f;

        public static IReadOnlyList<BrewSpot> Build(
            in CauldronProfileSettings bowl,
            in BrewLatticeSettings settings)
        {
            var spots = new List<BrewSpot>();
            float spacing = settings.BubbleRadius * 2f * settings.SpacingMargin;
            float y = bowl.WallThickness + settings.BubbleRadius + settings.FloorClearance;

            int rowIndex = 0;
            int overflowUsed = 0;
            while (true)
            {
                bool submerged = y + settings.BubbleRadius <= bowl.BowlDepth;
                if (!submerged)
                {
                    if (overflowUsed >= settings.OverflowLayers)
                    {
                        break;
                    }

                    overflowUsed++;
                }

                // Above the rim the bowl no longer narrows; sample the rim radius.
                float sampleHeight = Math.Min(y, bowl.BowlDepth);
                float innerRadius = CauldronProfileCalculator.InnerRadiusAtHeight(bowl, sampleHeight);
                float reach = innerRadius - settings.BubbleRadius - settings.EdgePadding;
                AddRow(spots, y, reach, spacing, rowIndex, settings.DepthJitter);

                y += spacing;
                rowIndex++;
            }

            return spots;
        }

        private static void AddRow(
            List<BrewSpot> spots,
            float y,
            float reach,
            float spacing,
            int rowIndex,
            float depthJitter)
        {
            // Centre-out: the row's centre column first, then alternating
            // right/left, so a partially filled row reads as a centred cluster.
            // Every row uses the same column grid (x = column * spacing), which
            // is what makes vertical columns — and the FR4 column settle — exist.
            for (int step = 0; ; step++)
            {
                int column = step % 2 == 0 ? step / 2 : -(step / 2 + 1);
                float x = column * spacing;
                if (Math.Abs(x) > reach)
                {
                    if (step % 2 == 0)
                    {
                        // The centre/right side ran out exactly where the left
                        // will too (the grid is symmetric); stop the row.
                        break;
                    }

                    continue;
                }

                AddSpot(spots, rowIndex, column, x, y, depthJitter);
            }
        }

        private static void AddSpot(
            List<BrewSpot> spots,
            int row,
            int column,
            float x,
            float y,
            float depthJitter)
        {
            int index = spots.Count;
            float jitter = (index * PlasticRatio % 1f * 2f - 1f) * depthJitter;
            spots.Add(new BrewSpot(index, row, column, x, y, jitter));
        }
    }
}
