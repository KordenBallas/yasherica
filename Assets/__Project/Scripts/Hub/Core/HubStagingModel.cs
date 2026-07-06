using System;
using System.Collections.Generic;
using LevelGeneration;

namespace Hub.Core
{
    /// <summary>
    /// The Hub's staging state (O1): the dealt starting-part offer, the two launch choices, and
    /// the launch signal. A part pick is optional — launching bare is always allowed (the kindless
    /// default and the cold-start case are the same state). Pure C# domain; the presenter drives it
    /// from view events and reads it at launch.
    /// </summary>
    public sealed class HubStagingModel
    {
        private static readonly IReadOnlyList<StartingPartCandidate> NoOffer =
            Array.Empty<StartingPartCandidate>();

        public IReadOnlyList<StartingPartCandidate> Offer { get; private set; } = NoOffer;

        /// <summary>The chosen candidate; null = launch bare.</summary>
        public StartingPartCandidate ChosenPart { get; private set; }

        public string ChosenPartId => ChosenPart != null ? ChosenPart.PartId : string.Empty;

        public LevelTheme ChosenBiome { get; private set; }

        public event Action<StartingPartCandidate> PartChosen;
        public event Action<LevelTheme> BiomeChosen;
        public event Action Launching;

        public void SetOffer(IReadOnlyList<StartingPartCandidate> offer)
        {
            Offer = offer ?? NoOffer;
        }

        /// <summary>Picks the offer card at <paramref name="index"/>; out-of-range picks are ignored.</summary>
        public bool ChoosePart(int index)
        {
            if (index < 0 || index >= Offer.Count)
            {
                return false;
            }

            ChosenPart = Offer[index];
            PartChosen?.Invoke(ChosenPart);
            return true;
        }

        public void ChooseBiome(LevelTheme biome)
        {
            ChosenBiome = biome;
            BiomeChosen?.Invoke(biome);
        }

        public void NotifyLaunching()
        {
            Launching?.Invoke();
        }
    }
}
