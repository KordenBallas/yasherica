namespace LevelGeneration.Journey
{
    /// <summary>
    /// Notified when the run crosses into a new biome stretch. The appearance-refresh seam: keeps
    /// LevelGeneration free of World.* references (like <c>IRouteLandmarkSpawner</c>) — the scene
    /// entrypoint implements it to swap landmark dressing and rebuild the backdrop.
    /// </summary>
    public interface IBiomeStretchObserver
    {
        void OnBiomeStretchChanged(BiomeStretch stretch);
    }
}
