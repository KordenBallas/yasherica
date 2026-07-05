namespace Inventory.View
{
    /// <summary>One row of the stored-parts readout: a friendly part name and how many
    /// copies the stash holds.</summary>
    public readonly struct PartInventoryItemViewData
    {
        public string DisplayName { get; }
        public int Count { get; }

        public PartInventoryItemViewData(string displayName, int count)
        {
            DisplayName = displayName;
            Count = count;
        }
    }
}
