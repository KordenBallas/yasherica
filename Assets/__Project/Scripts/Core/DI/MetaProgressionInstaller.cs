using MetaProgression.Core;
using MetaProgression.Data;
using UnityEngine;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Meta-progression bindings (Track R), shared by the Area and Hub scene contexts: the master
    /// tuning settings (SO → Core via the mapper; a missing asset degrades to defaults), the frozen
    /// per-scene <see cref="IMetaVocabulary"/> built from the meta store on disk (FR13 — a deed
    /// done this run changes the NEXT run's answers), and the cross-run direction
    /// <see cref="RunLedger"/> preloaded with the persisted past runs. Requires
    /// <c>PersistenceInstaller</c> (IMetaMemoryStore) on the same container.
    /// </summary>
    public static class MetaProgressionInstaller
    {
        private const string ConfigResourcePath = "Configs/MetaProgressionConfig";

        public static void Install(DiContainer container)
        {
            container.Bind<MetaProgressionSettings>()
                .FromMethod(_ => MetaProgressionConfigMapper.ToSettings(
                    Resources.Load<MetaProgressionConfig>(ConfigResourcePath)))
                .AsSingle();

            container.Bind<RunLedger>()
                .FromMethod(ctx => RunLedger.FromSnapshot(
                    ctx.Container.Resolve<Core.Persistence.IMetaMemoryStore>().LoadOrEmpty().Ledger))
                .AsSingle();

            container.Bind<IMetaVocabulary>()
                .FromMethod(ctx =>
                {
                    var snapshot = ctx.Container.Resolve<Core.Persistence.IMetaMemoryStore>().LoadOrEmpty();
                    return new MetaVocabulary(
                        snapshot.Facts,
                        ctx.Container.Resolve<MetaProgressionSettings>(),
                        EffectiveRunCount.From(snapshot.Facts, ServesFreshRun(ctx.Container)),
                        HeatLensOrNull(ctx.Container));
                })
                .AsSingle();
        }

        /// <summary>
        /// The Heat lens exists only where Track Y is installed (Hub + Area); Arena and menu scenes
        /// bind none, so their vocabulary answers exactly as before Track Y (min-Heat gates closed,
        /// no floor relief). Same binding-probe pattern as <see cref="ServesFreshRun"/>.
        /// </summary>
        private static IHeatLens HeatLensOrNull(DiContainer container)
        {
            return container.HasBinding<IHeatLens>() ? container.Resolve<IHeatLens>() : null;
        }

        /// <summary>
        /// The Hub always stages a fresh (upcoming) run; the Area serves a fresh run unless a
        /// pending restore says this boot continues the stored one. The restore context only
        /// exists on the Area container, hence the binding probe.
        /// </summary>
        private static bool ServesFreshRun(DiContainer container)
        {
            if (!container.HasBinding<Core.Persistence.RunRestoreContext>())
            {
                return true;
            }

            return !container.Resolve<Core.Persistence.RunRestoreContext>().IsRestoring;
        }
    }
}
