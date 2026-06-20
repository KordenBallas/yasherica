using Narrative.Actors.Core;

namespace Narrative.Actors.Data
{
    /// <summary>
    /// The only bridge from the <see cref="NpcArchetype"/> ScriptableObject to the UnityEngine-free
    /// <see cref="NpcArchetypeData"/> Core record. Visual assets (assembly/portrait) deliberately stay
    /// out of Core — they are consumed directly from the SO by the spawn layer.
    /// </summary>
    public static class NpcArchetypeMapper
    {
        public static NpcArchetypeData ToData(NpcArchetype archetype)
        {
            if (archetype == null)
            {
                return null;
            }

            return new NpcArchetypeData(
                archetype.ArchetypeId,
                archetype.DisplayNamePool,
                archetype.FactionId,
                archetype.BaseDisposition,
                archetype.ArchetypeTags);
        }
    }
}
