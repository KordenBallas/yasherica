namespace Core.SceneFlow
{
    /// <summary>
    /// Engine-free seam for switching scenes so presenters never touch
    /// <c>UnityEngine.SceneManagement</c> directly (testable, MVP-clean).
    /// </summary>
    public interface ISceneLoader
    {
        /// <summary>Loads the named scene, replacing the current one (single mode).</summary>
        void Load(string sceneName);
    }
}
