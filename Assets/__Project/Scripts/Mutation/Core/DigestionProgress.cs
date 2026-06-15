using System;

namespace Mutation.Core
{
    /// <summary>
    /// Default <see cref="IDigestionProgress"/>: a mutable per-stage count of digested artifacts
    /// against a fixed threshold. Pure C# (Core), so it stays free of UnityEngine and fully
    /// unit-testable (CLAUDE.md §2, §10). The threshold is supplied from authored data
    /// (MutationConfig) at install time - no magic numbers in code (CLAUDE.md §11).
    /// </summary>
    public sealed class DigestionProgress : IDigestionProgress
    {
        private readonly int _threshold;
        private int _fed;

        public DigestionProgress(int threshold)
        {
            if (threshold <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(threshold),
                    $"Digestion threshold must be positive, got {threshold}.");
            }

            _threshold = threshold;
        }

        public int Fed => _fed;

        public int Threshold => _threshold;

        public float Normalized => _fed >= _threshold ? 1f : (float)_fed / _threshold;

        public bool IsReadyToMutate => _fed >= _threshold;

        public event Action OnChanged;

        public void AddArtifact()
        {
            _fed++;
            OnChanged?.Invoke();
        }

        public void Reset()
        {
            if (_fed == 0)
            {
                return;
            }

            _fed = 0;
            OnChanged?.Invoke();
        }
    }
}
