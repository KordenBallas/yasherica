using System;

namespace Inventory.Core
{
    /// <summary>
    /// Maps the pot's artifact count to the liquid fill height (Track F
    /// fullness): a linear count curve between the configured min and max,
    /// raised when needed so the waterline always sits above the topmost
    /// bubble. The presenter snapshots the result when the view opens and
    /// holds it for the session.
    /// </summary>
    public static class LiquidFillCalculator
    {
        public static float Calculate(int itemCount, float highestBubbleTopY, in LiquidFillSettings settings)
        {
            if (itemCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(itemCount), "Item count must not be negative.");
            }

            if (itemCount == 0)
            {
                return settings.MinHeight;
            }

            float t = Math.Min(1f, itemCount / (float)settings.CountAtMax);
            float byCount = settings.MinHeight + (settings.MaxHeight - settings.MinHeight) * t;
            float aboveBubbles = highestBubbleTopY + settings.Headroom;
            float height = Math.Max(byCount, aboveBubbles);

            // The max clamp wins over bubble coverage: an overfilled pot keeps a
            // sane waterline instead of overflowing (documented overfill edge).
            return Math.Min(settings.MaxHeight, Math.Max(settings.MinHeight, height));
        }
    }
}
