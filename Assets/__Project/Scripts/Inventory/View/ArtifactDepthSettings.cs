namespace Inventory.View
{
    /// <summary>
    /// Tunables for the pseudo-3D artifact look: how many stacked sprite quads a
    /// bubble renders and how the rear layers recede. Values are mapped from
    /// InventoryConfig by the views that spawn bubbles.
    /// </summary>
    public readonly struct ArtifactDepthSettings
    {
        /// <summary>Total quad layers per artifact, front layer included.</summary>
        public int LayerCount { get; }

        /// <summary>Distance between consecutive layers along the quad's depth axis.</summary>
        public float LayerSpacing { get; }

        /// <summary>Brightness multiplier the rearmost layer fades to.</summary>
        public float BackLayerDarkening { get; }

        public ArtifactDepthSettings(int layerCount, float layerSpacing, float backLayerDarkening)
        {
            LayerCount = layerCount;
            LayerSpacing = layerSpacing;
            BackLayerDarkening = backLayerDarkening;
        }
    }
}
