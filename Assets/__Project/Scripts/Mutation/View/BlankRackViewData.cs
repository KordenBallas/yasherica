using System.Collections.Generic;
using UnityEngine;

namespace Mutation.View
{
    /// <summary>
    /// View DTO for one racked Part-Blank: everything the rack view needs to draw
    /// an entry (identity, label, species tint, socket layout, and what already
    /// sits in the sockets), with sprites/tints resolved by the presenter so the
    /// view stays logic-free.
    /// </summary>
    public sealed class BlankEntryViewData
    {
        public int BlankInstanceId { get; }
        public string DisplayName { get; }
        public Sprite Icon { get; }
        public Color SpeciesTint { get; }
        public int SocketCount { get; }
        public IReadOnlyList<SocketViewData> FilledSockets { get; }

        public BlankEntryViewData(
            int blankInstanceId,
            string displayName,
            Sprite icon,
            Color speciesTint,
            int socketCount,
            IReadOnlyList<SocketViewData> filledSockets)
        {
            BlankInstanceId = blankInstanceId;
            DisplayName = displayName;
            Icon = icon;
            SpeciesTint = speciesTint;
            SocketCount = socketCount;
            FilledSockets = filledSockets ?? System.Array.Empty<SocketViewData>();
        }
    }

    /// <summary>One filled socket: the socketed artifact's identity and look.</summary>
    public sealed class SocketViewData
    {
        public int ArtifactInstanceId { get; }
        public Sprite Icon { get; }
        public Color Tint { get; }

        public SocketViewData(int artifactInstanceId, Sprite icon, Color tint)
        {
            ArtifactInstanceId = artifactInstanceId;
            Icon = icon;
            Tint = tint;
        }
    }
}
