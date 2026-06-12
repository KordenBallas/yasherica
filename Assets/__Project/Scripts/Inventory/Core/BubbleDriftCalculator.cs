using System;

namespace Inventory.Core
{
    /// <summary>
    /// Deterministic idle motion for pot bubbles: a slow quasi-circular wander
    /// around each bubble's base point. Sampled per frame by the pot view.
    /// </summary>
    public static class BubbleDriftCalculator
    {
        private const float TwoPi = 6.2831853f;

        // Sqrt(2)/2: an irrational frequency ratio keeps the axes from phase-locking
        // into a fixed ellipse, so the path slowly precesses instead of repeating.
        private const float VerticalFrequencyRatio = 0.7071068f;

        // Quarter-turn offset makes near-equal axis frequencies trace a circle
        // around the base point rather than a diagonal line through it.
        private const float VerticalPhaseOffset = 1.5707963f;

        // Scrambles per-bubble phases differently on each axis so neighbouring
        // bubbles desync in both dimensions, not just along one.
        private const float VerticalPhaseScramble = 1.7f;

        public static void SampleOffset(
            float time, float phase, in BubbleDriftSettings settings,
            out float offsetX, out float offsetY)
        {
            if (settings.Amplitude <= 0f)
            {
                offsetX = 0f;
                offsetY = 0f;
                return;
            }

            float omega = TwoPi * settings.Frequency;
            offsetX = settings.Amplitude * (float)Math.Sin(omega * time + phase);
            offsetY = settings.Amplitude * (float)Math.Sin(
                omega * time * VerticalFrequencyRatio + phase * VerticalPhaseScramble + VerticalPhaseOffset);
        }
    }
}
