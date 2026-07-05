using System.Collections.Generic;

namespace CharacterSystem.Core
{
    /// <summary>
    /// Where parts shed by a body-plan change go. Owned by the character system so its
    /// runtime never references the Inventory layer; the Inventory side implements this
    /// port (mirroring the Mutation-side adapter direction). Shed parts are returned,
    /// never destroyed (body-plan-skeleton-swap.md FR8).
    /// </summary>
    public interface IShedPartSink
    {
        void Store(IReadOnlyList<string> partIds);
    }
}
