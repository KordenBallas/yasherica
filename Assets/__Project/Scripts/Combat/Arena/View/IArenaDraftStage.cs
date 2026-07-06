using System.Collections.Generic;
using Combat.Arena.Core;
using UnityEngine;

namespace Combat.Arena.View
{
    /// <summary>
    /// The draft screen's 3D half: the slot-grouped board of part models + the local player's
    /// live-assembling monster, rendered to one texture. The presenter drives it and resolves
    /// clicks through it; the uGUI view only shows the texture and relays pointer UVs.
    /// </summary>
    public interface IArenaDraftStage
    {
        Texture Texture { get; }

        void Build(IReadOnlyList<ArenaDraftBoardEntry> board, IReadOnlyList<string> slotLoadout);

        void UpdateLocalHero(IReadOnlyDictionary<string, string> loadout);

        void RemoveEntry(int entryId);

        bool TryRaycast(Vector2 viewportUv, out ArenaDraftStageHit hit);

        /// <summary>Where a board entry sits in the stage camera's viewport (for pick flights).</summary>
        bool TryGetEntryViewportPoint(int entryId, out Vector2 viewportUv);

        void Clear();
    }
}
