using System;

namespace Mutation.Core
{
    /// <summary>
    /// Per-stage digestion progress: how many artifacts have been fed toward the threshold that
    /// makes the character ready to mutate to the next stage. UnityEngine-free and unit-testable.
    /// The tally (<see cref="IMutationTally"/>) decides WHICH archetype the mutation leans toward;
    /// this decides WHEN a mutation becomes available. <see cref="Reset"/> starts a fresh stage.
    /// </summary>
    public interface IDigestionProgress
    {
        /// <summary>Artifacts fed during the current mutation stage.</summary>
        int Fed { get; }

        /// <summary>Artifacts that must be fed before the stage is ready to mutate.</summary>
        int Threshold { get; }

        /// <summary><see cref="Fed"/> over <see cref="Threshold"/>, clamped to 0..1.</summary>
        float Normalized { get; }

        /// <summary>True once enough artifacts have been fed to mutate to the next stage.</summary>
        bool IsReadyToMutate { get; }

        /// <summary>Raised whenever the progress actually changes (an add, or a non-empty reset).</summary>
        event Action OnChanged;

        /// <summary>Records that one artifact was digested this stage.</summary>
        void AddArtifact();

        /// <summary>Clears the digested count so a new mutation stage starts from zero.</summary>
        void Reset();
    }
}
