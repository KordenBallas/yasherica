namespace Combat.Arena.Core
{
    /// <summary>
    /// Resolves a tasted-catalog part id to its draftable info (id + slot). Seams the part
    /// catalog out of Core: the production source wraps IPartCatalog, tests fake it.
    /// </summary>
    public interface IArenaDraftPartInfoSource
    {
        bool TryGet(string partId, out ArenaDraftPartInfo info);
    }
}
