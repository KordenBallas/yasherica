using System;
using System.Collections.Generic;

namespace Mutation.Core
{
    /// <summary>
    /// Live, UnityEngine-free aggregate of how strongly the artifacts fed during the current
    /// mutation stage pull the character toward each creature archetype (archetype id -> weight).
    /// Fed via <see cref="Add"/> from <see cref="ArtifactArchetypeProfile"/> snapshots; read by the
    /// feeding UI and the stage-up mutation choice. <see cref="Reset"/> starts a fresh stage.
    /// </summary>
    public interface IMutationTally
    {
        /// <summary>Archetype id -> accumulated weight for the current mutation stage.</summary>
        IReadOnlyDictionary<string, float> Totals { get; }

        bool IsEmpty { get; }

        /// <summary>Raised whenever the totals actually change (an effective add, or a reset).</summary>
        event Action OnChanged;

        /// <summary>Sums the profile's weights into the current stage. Null/empty profiles are no-ops.</summary>
        void Add(ArtifactArchetypeProfile profile);

        /// <summary>Accumulated weight for <paramref name="archetypeId"/>, or 0 if none / null / empty.</summary>
        float TotalFor(string archetypeId);

        /// <summary>
        /// The dominant archetype ids, ordered by descending weight with a deterministic
        /// ordinal-id tie-break. <paramref name="count"/> is clamped to the number of tracked
        /// archetypes; an empty tally yields an empty list.
        /// </summary>
        IReadOnlyList<string> Dominant(int count);

        /// <summary>Clears the stage tally so a new mutation stage starts from zero.</summary>
        void Reset();
    }
}
