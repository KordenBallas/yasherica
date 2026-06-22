namespace Narrative.Actors.Data
{
    /// <summary>
    /// Resolves an archetype id (carried on a minted <c>NpcInstance</c>) back to its
    /// <see cref="NpcArchetype"/> SO so the spawn layer can read the visual assembly and portrait —
    /// the bridge the UnityEngine-free <c>NpcArchetypeData</c> deliberately omits.
    /// </summary>
    public interface INpcArchetypeCatalog
    {
        NpcArchetype Get(string archetypeId);
    }
}
