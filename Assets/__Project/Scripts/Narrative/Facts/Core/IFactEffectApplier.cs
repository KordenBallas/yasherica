using System.Collections.Generic;

namespace Narrative.Facts.Core
{
    /// <summary>
    /// Applies fact effects uniformly across namespaces (R9) and is the single runtime write chokepoint
    /// for authored/play-time effects (the footprint guard, W2-1). Each call validates the write against
    /// the *writing fragment's own* footprint of <see cref="FactKeyShapeCore"/> before mutating the store.
    /// </summary>
    public interface IFactEffectApplier
    {
        /// <summary>
        /// Applies one effect if it passes the op×type and footprint guards. <paramref name="footprint"/>
        /// is the writing fragment's declared write shapes; <paramref name="runtimeOverride"/> supplies an
        /// exact play-time value (e.g. from an Ink <c>fact:</c> tag) while the template still declared the
        /// footprint (R7). Returns true if the store was mutated.
        /// </summary>
        bool Apply(FactEffectCore effect, IFactStore store, ISubjectContext context,
            IReadOnlyCollection<FactKeyShapeCore> footprint, FactValue? runtimeOverride = null);

        void ApplyAll(IReadOnlyList<FactEffectCore> effects, IFactStore store, ISubjectContext context,
            IReadOnlyCollection<FactKeyShapeCore> footprint);
    }
}
