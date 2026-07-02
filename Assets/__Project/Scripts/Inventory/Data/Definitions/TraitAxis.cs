namespace Inventory.Data.Definitions
{
    /// <summary>
    /// The authoring axis a trait belongs to: what an artifact is made of
    /// (Substance) or what it does (Property). The runtime grammar is
    /// axis-agnostic; the axis exists for authoring clarity and validation.
    /// </summary>
    public enum TraitAxis
    {
        Substance = 0,
        Property = 1
    }
}
