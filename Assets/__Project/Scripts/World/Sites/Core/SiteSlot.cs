namespace World.Sites.Core
{
    /// <summary>One platform slot of a built site block: the beat it carries and its site stamp.</summary>
    public readonly struct SiteSlot
    {
        public SiteSlot(ContentBeat beat, SiteStamp stamp)
        {
            Beat = beat;
            Stamp = stamp;
        }

        public ContentBeat Beat { get; }
        public SiteStamp Stamp { get; }
    }
}
