namespace World.Sites.Core
{
    /// <summary>
    /// One named world beat in the shared <c>base·flavor</c> vocabulary: a stable
    /// <see cref="ContentBaseKind"/> refined by an open flavor tag (empty = unflavored, e.g. plain
    /// traversal Empty or the default biome loot). Site recipes and density budgets both speak this
    /// vocabulary, so they never drift apart.
    /// </summary>
    public readonly struct ContentBeat
    {
        public ContentBeat(ContentBaseKind kind, string flavor = null)
        {
            Kind = kind;
            Flavor = flavor ?? string.Empty;
        }

        public ContentBaseKind Kind { get; }

        /// <summary>The flavor tag (e.g. "market", "bandit", "townsfolk"); empty when unflavored.</summary>
        public string Flavor { get; }
    }
}
