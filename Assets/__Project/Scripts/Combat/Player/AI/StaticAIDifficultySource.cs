namespace Combat.Player.AI
{
    /// <summary>
    /// Install-time difficulty snapshot: whatever DifficultyDefinition the installer mapped
    /// (or Neutral when none is configured) holds for the whole scene lifetime.
    /// </summary>
    public sealed class StaticAIDifficultySource : IAIDifficultySource
    {
        public StaticAIDifficultySource(AIDifficultySettings settings)
        {
            Current = settings ?? AIDifficultySettings.Neutral;
        }

        public AIDifficultySettings Current { get; }
    }
}
