using System.IO;
using Core.Logging;
using Core.Persistence;
using UnityEngine;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Binds the save-file stores (run save + cross-run memory) over
    /// <c>persistentDataPath/Saves</c>. Installed by every scene that reads or writes saves
    /// (Area, MainMenu); the file on disk — not a cross-scene container — is the carrier between
    /// scenes (D3). Run-scoped persistence services (restore/autosave/lifecycle) are Area-only and
    /// bound by <see cref="AreaInstaller"/>, not here.
    /// </summary>
    public class PersistenceInstaller : Installer<PersistenceInstaller>
    {
        private const string SavesFolderName = "Saves";

        public override void InstallBindings()
        {
            var directory = Path.Combine(Application.persistentDataPath, SavesFolderName);

            Container.Bind<ISaveSerializer>().To<UnityJsonSaveSerializer>().AsSingle();
            Container.Bind<IRunSaveStore>()
                .FromMethod(ctx => new RunSaveStore(directory,
                    ctx.Container.Resolve<ISaveSerializer>(),
                    ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();
            Container.Bind<IMetaMemoryStore>()
                .FromMethod(ctx => new MetaMemoryStore(directory,
                    ctx.Container.Resolve<ISaveSerializer>(),
                    ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();

            // O1 one-shot carriers: the Hub → Area launch setup and the Area → Hub arrival marker.
            Container.Bind<IRunSetupStore>()
                .FromMethod(ctx => new RunSetupStore(directory,
                    ctx.Container.Resolve<ISaveSerializer>(),
                    ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();
            Container.Bind<IHubArrivalStore>()
                .FromMethod(ctx => new HubArrivalStore(directory,
                    ctx.Container.Resolve<ISaveSerializer>(),
                    ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();
        }
    }
}
