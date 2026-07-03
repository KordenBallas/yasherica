namespace Combat.Player
{
    /// <summary>
    /// View seam for the ghost telegraph playback. One ghost at a time: Play cancels any
    /// running playback; the view owns cloning, translucency, and the fade lifecycle.
    /// </summary>
    public interface IGhostPlaybackView
    {
        void Play(GhostPlaybackPlan plan);
        void Stop();
    }
}
