using UnityEngine;

namespace Core.Camera
{
    /// <summary>
    /// Supplies the world anchor the belly camera follows while the inventory is
    /// open. Lives on the hero so the framing tracks the character.
    /// </summary>
    public interface IBellyAnchorProvider
    {
        Transform BellyAnchor { get; }
    }
}
