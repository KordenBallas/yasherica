using System.Collections.Generic;

namespace Heat.Core
{
    /// <summary>
    /// Pure image of the Heat config asset (heat-ascension "Tuning surface"): the modifier menu plus
    /// the dials mapping total Heat onto Track R's pacing and dig bias. Validated here so no authored
    /// configuration can break the invariants: all dials clamp non-negative, and the never-guarantee
    /// ceiling is untouchable by design — Heat only lifts <c>BiasStrength</c>, never the ceiling
    /// (see <see cref="HeatDialAdjuster"/>).
    /// </summary>
    public sealed class HeatSettings
    {
        // Declared before Defaults: static initializers run in declaration order, and the
        // Defaults constructor falls back onto this list.
        private static readonly IReadOnlyList<HeatModifier> EmptyModifiers = new HeatModifier[0];

        /// <summary>An inert system: empty menu, all dials zero — Heat absent reproduces today's game (FR13).</summary>
        public static readonly HeatSettings Defaults = new HeatSettings(
            modifiers: null,
            floorReliefRunsPerHeat: 0f,
            biasStrengthLiftPerHeat: 0f,
            reserveDirectionSlotMinHeat: 0,
            softCapTotalHeat: 0,
            clearWindowFloor: 1);

        /// <summary>The authored modifier menu (FR1).</summary>
        public IReadOnlyList<HeatModifier> Modifiers { get; }

        /// <summary>Runs of tier run-floor relief per point of total Heat (FR6's pacing acceleration).</summary>
        public float FloorReliefRunsPerHeat { get; }

        /// <summary>Additive lift to R's dig <c>BiasStrength</c> per point of total Heat (FR6's dig floor).</summary>
        public float BiasStrengthLiftPerHeat { get; }

        /// <summary>Total Heat at which the dig reserves a direction slot; 0 = Heat never enables it.</summary>
        public int ReserveDirectionSlotMinHeat { get; }

        /// <summary>The soft cap on total Heat the hub pact UI refuses to exceed; 0 = uncapped.</summary>
        public int SoftCapTotalHeat { get; }

        /// <summary>Window index a hot run must reach at a savepoint to count as "cleared" (the MVP high-water criterion).</summary>
        public int ClearWindowFloor { get; }

        public HeatSettings(
            IReadOnlyList<HeatModifier> modifiers,
            float floorReliefRunsPerHeat,
            float biasStrengthLiftPerHeat,
            int reserveDirectionSlotMinHeat,
            int softCapTotalHeat,
            int clearWindowFloor)
        {
            Modifiers = modifiers ?? EmptyModifiers;
            FloorReliefRunsPerHeat = floorReliefRunsPerHeat < 0f ? 0f : floorReliefRunsPerHeat;
            BiasStrengthLiftPerHeat = biasStrengthLiftPerHeat < 0f ? 0f : biasStrengthLiftPerHeat;
            ReserveDirectionSlotMinHeat = reserveDirectionSlotMinHeat < 0 ? 0 : reserveDirectionSlotMinHeat;
            SoftCapTotalHeat = softCapTotalHeat < 0 ? 0 : softCapTotalHeat;
            ClearWindowFloor = clearWindowFloor < 0 ? 0 : clearWindowFloor;
        }

        /// <summary>Runs subtracted from a gated token's effective tier run-floor at <paramref name="totalHeat"/>.</summary>
        public int FloorReliefFor(int totalHeat)
        {
            if (totalHeat <= 0)
            {
                return 0;
            }

            return (int)(totalHeat * FloorReliefRunsPerHeat);
        }

        public bool TryGetModifier(string id, out HeatModifier modifier)
        {
            for (int i = 0; i < Modifiers.Count; i++)
            {
                if (Modifiers[i].Id == id)
                {
                    modifier = Modifiers[i];
                    return true;
                }
            }

            modifier = null;
            return false;
        }
    }
}
