namespace Hub.View
{
    /// <summary>
    /// Adapter contract for the Hub's screen-space staging readout (O1 rework): what the launch
    /// will install ("—" for a bare launch). The picks themselves happen in the WORLD — the
    /// junk-keeper NPC deals the cards, the labelled portals launch — so this view carries no
    /// buttons.
    /// </summary>
    public interface IHubStagingView
    {
        /// <summary>Shows what the launch will install ("—" for a bare launch).</summary>
        void SetChosenPartLabel(string label);
    }
}
