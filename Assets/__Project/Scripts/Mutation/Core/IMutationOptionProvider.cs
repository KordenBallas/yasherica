using System.Collections.Generic;

namespace Mutation.Core
{
    /// <summary>
    /// Source of the candidate mutation options each archetype can grant. UnityEngine-free so the
    /// option builder stays unit-testable; the Data layer's option catalog implements it over the
    /// authored part-set assets. Returns options in authored order; never null.
    /// </summary>
    public interface IMutationOptionProvider
    {
        /// <summary>
        /// The body-part options authored for <paramref name="archetypeId"/>, in authored order.
        /// Unknown or empty ids yield an empty list.
        /// </summary>
        IReadOnlyList<MutationOption> OptionsFor(string archetypeId);
    }
}
