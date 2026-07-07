namespace Inventory.Core
{
    /// <summary>
    /// Source of the authored cauldron bowl dimensions, so the brew lattice and
    /// the liquid surface derive their geometry from the one view that owns the
    /// pot silhouette instead of duplicating size tunables.
    /// </summary>
    public interface ICauldronGeometry
    {
        CauldronProfileSettings ProfileSettings { get; }
    }
}
