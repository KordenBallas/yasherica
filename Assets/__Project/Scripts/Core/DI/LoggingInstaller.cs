using Core.Logging;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// The single home for the game logger. Installed once per container by <see cref="AreaInstaller"/>
    /// (a non-Mono <see cref="Installer{TDerived}"/>, so it needs no GameObject). Every other installer
    /// only RESOLVES <see cref="IGameLogger"/> and must never bind it: Zenject 6 forbids AsSingle on the
    /// same concrete type across two bindings, and <c>IfNotBound</c> does not prevent it (the clash is on
    /// the concrete-type singleton mark, not the contract).
    /// </summary>
    public class LoggingInstaller : Installer<LoggingInstaller>
    {
        public override void InstallBindings()
        {
            Container.Bind<IGameLogger>().To<UnityGameLogger>().AsSingle();
        }
    }
}
