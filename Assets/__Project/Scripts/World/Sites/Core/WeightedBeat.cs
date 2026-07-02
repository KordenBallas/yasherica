namespace World.Sites.Core
{
    /// <summary>One weighted entry of a site's fill table: a <see cref="ContentBeat"/> and its draw weight.</summary>
    public readonly struct WeightedBeat
    {
        public WeightedBeat(ContentBeat beat, int weight)
        {
            Beat = beat;
            Weight = weight < 0 ? 0 : weight;
        }

        public ContentBeat Beat { get; }
        public int Weight { get; }
    }
}
