using Core.Logging;
using UnityEngine;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// The single home for the game logger. Installed once per container by <see cref="AreaInstaller"/>
    /// (a non-Mono <see cref="Installer{TDerived}"/>, so it needs no GameObject). Every other installer
    /// only RESOLVES <see cref="IGameLogger"/> and must never bind it: Zenject 6 forbids AsSingle on the
    /// same concrete type across two bindings, and <c>IfNotBound</c> does not prevent it (the clash is on
    /// the concrete-type singleton mark, not the contract).
    ///
    /// Loads the per-system <see cref="LoggingConfig"/> from Resources and binds the derived
    /// <see cref="LogLevelPolicy"/> the logger consults. A missing asset falls back to "everything on"
    /// so logging never hard-fails.
    /// </summary>
    public class LoggingInstaller : Installer<LoggingInstaller>
    {
        private const string ConfigResourcePath = "Configs/LoggingConfig";

        public override void InstallBindings()
        {
            var config = Resources.Load<LoggingConfig>(ConfigResourcePath);
            var policy = config != null ? config.ToPolicy() : LogLevelPolicy.AllEnabled();

            if (config == null)
            {
                Debug.LogWarning(
                    $"[LoggingInstaller] No LoggingConfig at Resources/{ConfigResourcePath}; " +
                    "logging every system at Info. Create one via Assets > Create > Config > Logging Config.");
            }

            Container.Bind<LogLevelPolicy>().FromInstance(policy).AsSingle();
            Container.Bind<IGameLogger>().To<UnityGameLogger>().AsSingle();
        }
    }
}
