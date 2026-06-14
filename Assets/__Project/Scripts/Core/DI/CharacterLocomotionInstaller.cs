using Character.Locomotion;
using UnityEngine;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Wires the movement-driven locomotion subsystem: the velocity provider (the player's
    /// movement controller) and the locomotion view (on the Hero root) are resolved from the
    /// scene hierarchy, the pure solver is built from <see cref="LocomotionConfig"/>, and the
    /// presenter is registered as an <see cref="ITickable"/> so Zenject drives it each frame.
    /// </summary>
    public class CharacterLocomotionInstaller : MonoInstaller
    {
        private const string LocomotionConfigResourcePath = "CharacterSystem/LocomotionConfig";

        [Header("Configuration (auto-loaded from Resources when empty)")]
        [SerializeField] private LocomotionConfig _config;

        public override void InstallBindings()
        {
            var config = ResolveConfig();

            Container.Bind<LocomotionSolver>()
                .FromInstance(new LocomotionSolver(config.MoveThresholdSpeed, config.TurnDegreesPerSecond))
                .AsSingle();

            Container.Bind<ICharacterVelocityProvider>()
                .FromComponentInHierarchy()
                .AsSingle();

            Container.Bind<ILocomotionView>()
                .FromComponentInHierarchy()
                .AsSingle();

            Container.BindInterfacesAndSelfTo<CharacterLocomotionPresenter>()
                .AsSingle()
                .NonLazy();
        }

        private LocomotionConfig ResolveConfig()
        {
            if (_config != null)
            {
                return _config;
            }

            var loaded = Resources.Load<LocomotionConfig>(LocomotionConfigResourcePath);
            if (loaded != null)
            {
                return loaded;
            }

            // Fall back to a default instance (uses the SO's serialized default values) so the
            // scene still runs; warn so the missing asset gets authored.
            Debug.LogWarning(
                $"[CharacterLocomotionInstaller] No LocomotionConfig assigned or found at Resources/{LocomotionConfigResourcePath}; using defaults.");
            return ScriptableObject.CreateInstance<LocomotionConfig>();
        }
    }
}
