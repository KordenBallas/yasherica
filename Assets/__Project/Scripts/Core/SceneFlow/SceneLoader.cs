using UnityEngine.SceneManagement;

namespace Core.SceneFlow
{
    /// <summary>
    /// Infrastructure adapter over <see cref="SceneManager"/>. Plain single-mode load is enough:
    /// every scene carries its own self-contained Zenject SceneContext, so no cross-scene
    /// container plumbing (ZenjectSceneLoader) is needed.
    /// </summary>
    public class SceneLoader : ISceneLoader
    {
        public void Load(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }
}
