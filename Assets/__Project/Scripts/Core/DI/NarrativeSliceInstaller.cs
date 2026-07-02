using System.Collections.Generic;
using CharacterProgression.Core;
using Combat.Data;
using Combat.Data.Definitions;
using Core.Logging;
using Narrative;
using Narrative.Actors.Core;
using Narrative.Actors.Data;
using Narrative.Casting.Core;
using Narrative.Dialogue;
using Narrative.Dialogue.Core;
using Narrative.Dialogue.Data;
using Narrative.Encounter;
using Narrative.Director.Core;
using Narrative.Director.Data;
using Narrative.Facts.Core;
using Narrative.Facts.Data;
using Narrative.Quests.Core;
using Narrative.Quests.Data;
using Narrative.Runtime.Core;
using Narrative.Runtime.Snapshots;
using Narrative.Stories.Core;
using Narrative.Stories.Data;
using Narrative.View;
using UnityEngine;
using Zenject;
using DataFactRegistry = Narrative.Facts.Data.FactKeyRegistry;

namespace Core.DI
{
    /// <summary>
    /// Zenject installer for the data-driven procedural narrative system (now the only narrative path).
    /// Wires the unified fact store, precondition/effect machinery, casting factory, run director,
    /// windowed planner, dialogue runner + view, and save boundary from authored ScriptableObjects.
    /// Auto-loads the Demo content from <c>Resources/Narrative/*</c> when the inspector lists are empty.
    /// </summary>
    public class NarrativeSliceInstaller : MonoInstaller
    {
        private const string SeedContextKey = "narrative-slice";
        private const string StoryManagerId = "narrative-slice";

        [Header("Fact Vocabulary")]
        [SerializeField] private DataFactRegistry _factKeyRegistry;

        [Header("Fragment Library")]
        [SerializeField] private List<NpcArchetype> _archetypes = new List<NpcArchetype>();
        [SerializeField] private List<DialogueDefinition> _dialogues = new List<DialogueDefinition>();
        [SerializeField] private List<QuestDefinition> _quests = new List<QuestDefinition>();
        [SerializeField] private List<EnemyDefinition> _enemies = new List<EnemyDefinition>();

        [Header("Storylets")]
        [SerializeField] private List<StoryTemplate> _storyTemplates = new List<StoryTemplate>();

        [Header("Director Pacing")]
        [SerializeField] private RunPacingConfig _runPacingConfig;

        [Header("World Content Density")]
        [SerializeField] private WorldContentDensityConfig _worldContentDensityConfig;
        [SerializeField] private List<BiomeMonsterPoolDefinition> _biomeMonsterPools = new List<BiomeMonsterPoolDefinition>();

        [Header("World Sites")]
        [SerializeField] private List<World.Sites.Data.SiteDefinition> _siteDefinitions = new List<World.Sites.Data.SiteDefinition>();

        public override void InstallBindings()
        {
            // IGameLogger is provided by LoggingInstaller (installed by AreaInstaller); we only resolve
            // it. Re-binding UnityGameLogger AsSingle here would trip Zenject 6's "AsSingle multiple
            // times for the same concrete type" assert (IfNotBound does NOT prevent it).

            ResolveAssetsFromResources();
            InstallFacts();
            InstallFragmentsAndStorylets();
            InstallDirectorAndCasting();
            InstallPlanner();
            InstallView();
            InstallDialogue();
            InstallSave();

            // Caches story footprints and validates typed refs after the container is built.
            Container.BindInterfacesTo<NarrativeSliceBootstrap>().AsSingle().NonLazy();
        }

        private void InstallFacts()
        {
            var registry = FactKeyRegistryMapper.ToRegistry(_factKeyRegistry);
            Container.Bind<IFactKeyRegistry>().FromInstance(registry).AsSingle();

            Container.Bind<IFactStore>()
                .To<FactStore>()
                .FromMethod(ctx => new FactStore(registry, ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();

            Container.Bind<ISubjectResolver>().To<SubjectResolver>().AsSingle();
            Container.Bind<IPreconditionEvaluator>().To<PreconditionEvaluator>().AsSingle();
            Container.Bind<IFactEffectApplier>().To<FactEffectApplier>().AsSingle();
        }

        private void InstallFragmentsAndStorylets()
        {
            Container.Bind<IReadOnlyList<NpcArchetypeData>>().FromInstance(MapArchetypes()).AsSingle();
            Container.Bind<IFragmentLibrary>().FromInstance(BuildFragmentLibrary()).AsSingle();
            Container.Bind<IReadOnlyList<StoryTemplateData>>().FromInstance(MapStorylets()).AsSingle();
        }

        private void InstallDirectorAndCasting()
        {
            Container.Bind<IRandomSource>()
                .FromMethod(ctx => new DeterministicRandom(unchecked((ulong)CreateSeed(ctx.Container))))
                .AsSingle();

            Container.Bind<ICastingFactory>()
                .To<CastingFactory>()
                .FromMethod(ctx => new CastingFactory(ctx.Container.Resolve<IRandomSource>(), ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();

            Container.Bind<IRunDirector>()
                .To<RunDirector>()
                .FromMethod(ctx => new RunDirector(ctx.Container.Resolve<IPreconditionEvaluator>(), ctx.Container.Resolve<IRandomSource>()))
                .AsSingle();

            // Mints a run-stable NpcInstance per placed actor (R12); draws names from the same seeded
            // stream as the director/casting so the whole run is replay-deterministic.
            Container.Bind<IActorInstanceFactory>()
                .To<ActorInstanceFactory>()
                .FromMethod(ctx => new ActorInstanceFactory(ctx.Container.Resolve<IRandomSource>()))
                .AsSingle();

            // Run-scoped set of minted actors the planner queries to recast a recurring actor (D11/D16).
            Container.Bind<ILiveActorRegistry>().To<LiveActorRegistry>().AsSingle();

            // Run-scoped set of offered quests the runner restores by id for cross-dialogue continuity,
            // and the reward granter scans for a completed-but-unpaid quest on platform completion.
            Container.Bind<ILiveQuestRegistry>().To<LiveQuestRegistry>().AsSingle();

            // The production orchestration (SelectNext -> Cast -> DialogueRunner.Begin). Bound lazily;
            // the gameplay trigger that calls BeginEncounter lands in a later cutover stage.
            Container.Bind<EncounterDirector>()
                .FromMethod(ctx => new EncounterDirector(
                    ctx.Container.Resolve<IRunDirector>(),
                    ctx.Container.Resolve<ICastingFactory>(),
                    ctx.Container.Resolve<IFragmentLibrary>(),
                    ctx.Container.Resolve<IReadOnlyList<StoryTemplateData>>(),
                    ctx.Container.Resolve<IFactStore>(),
                    ctx.Container.Resolve<DialogueRunner>()))
                .AsSingle();
        }

        private void InstallPlanner()
        {
            // Window mechanics (windowed director). Falls back to defaults if no config wired.
            Container.Bind<RunPacingSettings>()
                .FromInstance(RunPacingConfigMapper.ToSettings(_runPacingConfig))
                .AsSingle();

            // World fullness (quest rarity/spacing + ambient empty/loot/combat mix). One asset, mapped
            // to the UnityEngine-free settings record at install time.
            Container.Bind<WorldContentDensitySettings>()
                .FromInstance(WorldContentDensityConfigMapper.ToSettings(_worldContentDensityConfig))
                .AsSingle();

            // Ambient monster pools per biome (Combat·wild-beast, flat difficulty).
            Container.Bind<IBiomeMonsterPoolCatalog>()
                .To<BiomeMonsterPoolCatalog>()
                .FromMethod(ctx => new BiomeMonsterPoolCatalog(
                    BiomeMonsterPoolMapper.ToPools(_biomeMonsterPools, ctx.Container.Resolve<IGameLogger>())))
                .AsSingle();

            // The authored site vocabulary (world-sites brief), family defaults merged at install time.
            // An empty catalog keeps the allocation path a pure passthrough (no sites in the world).
            Container.Bind<World.Sites.Core.ISiteCatalog>()
                .FromMethod(ctx => World.Sites.Data.SiteCatalogMapper.ToCatalog(
                    _siteDefinitions, ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();

            Container.Bind<World.Sites.Core.SiteBlockBuilder>().AsSingle();

            // Run-scoped slot allocator: owns the quest-spacing counter across windows and shares the
            // director's seeded stream so allocation is replay-deterministic (B2).
            Container.Bind<WorldContentAllocator>()
                .FromMethod(ctx => new WorldContentAllocator(
                    ctx.Container.Resolve<WorldContentDensitySettings>(),
                    ctx.Container.Resolve<IBiomeMonsterPoolCatalog>(),
                    ctx.Container.Resolve<Loot.Core.ICurrentThemeProvider>(),
                    ctx.Container.Resolve<IRandomSource>(),
                    ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();

            // id -> archetype SO, so the spawn layer can read the visual assembly/portrait.
            Container.Bind<INpcArchetypeCatalog>()
                .FromInstance(new NpcArchetypeCatalog(_archetypes))
                .AsSingle();

            Container.Bind<IRunWindowPlanner>()
                .To<RunWindowPlanner>()
                .FromMethod(ctx => new RunWindowPlanner(
                    ctx.Container.Resolve<IReadOnlyList<StoryTemplateData>>(),
                    ctx.Container.Resolve<IReadOnlyList<NpcArchetypeData>>(),
                    ctx.Container.Resolve<IPreconditionEvaluator>(),
                    ctx.Container.Resolve<IActorInstanceFactory>(),
                    ctx.Container.Resolve<ILiveActorRegistry>(),
                    ctx.Container.Resolve<IRandomSource>(),
                    ctx.Container.Resolve<RunPacingSettings>(),
                    ctx.Container.Resolve<WorldContentAllocator>(),
                    ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();
        }

        private void InstallView()
        {
            // The encounter card-hand view (the active encounter presentation; replaces the line-reading
            // IDialogueView). Instantiated from the Resources prefab; EncounterCardHandPresenter drives it
            // from the runner's events. The old DialogueView/IDialogueView is left dormant (unbound) for
            // the separate "Remove the branching-choice dialogue UI" follow-up.
            Container.Bind<IEncounterCardHandView>()
                .To<EncounterCardHandView>()
                .FromComponentInNewPrefabResource("Prefabs/UI/Encounter/EncounterCardHandView")
                .AsSingle()
                .NonLazy();
        }

        private void InstallDialogue()
        {
            // The runner's Ink story manager, id'd for clarity.
            Container.Bind<IStoryManager>().WithId(StoryManagerId).To<InkStoryManager>().AsCached();

            Container.Bind<DialogueSession>()
                .FromMethod(ctx => new DialogueSession(ctx.Container.ResolveId<IStoryManager>(StoryManagerId)))
                .AsSingle();

            Container.Bind<DialogueTagParser>()
                .FromMethod(ctx => new DialogueTagParser(ctx.Container.Resolve<IFactKeyRegistry>(), ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();

            Container.Bind<DialogueRunner>()
                .FromMethod(ctx => new DialogueRunner(
                    ctx.Container.Resolve<DialogueSession>(),
                    ctx.Container.Resolve<IFactStore>(),
                    ctx.Container.Resolve<IFactEffectApplier>(),
                    ctx.Container.Resolve<DialogueTagParser>(),
                    ctx.Container.Resolve<IRunProgressionRecorder>(),
                    ctx.Container.Resolve<ILiveQuestRegistry>(),
                    ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();

            // Card-hand presenter: composes the typed card hand from the runner's events and drives the
            // IEncounterCardHandView. Replaces DialogueRunnerViewPresenter as the active encounter adapter
            // (the old presenter is left dormant — unbound — for the separate UI-removal follow-up).
            Container.BindInterfacesTo<EncounterCardHandPresenter>().AsSingle().NonLazy();
        }

        private void InstallSave()
        {
            Container.Bind<INarrativeSaveService>()
                .To<NarrativeSaveService>()
                .FromMethod(ctx => new NarrativeSaveService(
                    ctx.Container.Resolve<IFactStore>(),
                    ctx.Container.Resolve<IRandomSource>(),
                    CreateSeed(ctx.Container)))
                .AsSingle();
        }

        private List<NpcArchetypeData> MapArchetypes()
        {
            var result = new List<NpcArchetypeData>();
            foreach (var archetype in _archetypes)
            {
                var data = NpcArchetypeMapper.ToData(archetype);
                if (data != null)
                {
                    result.Add(data);
                }
            }

            return result;
        }

        private List<StoryTemplateData> MapStorylets()
        {
            var result = new List<StoryTemplateData>();
            foreach (var template in _storyTemplates)
            {
                var data = StoryTemplateMapper.ToData(template);
                if (data != null)
                {
                    result.Add(data);
                }
            }

            return result;
        }

        private FragmentLibrary BuildFragmentLibrary()
        {
            var dialogues = new List<DialogueData>();
            foreach (var d in _dialogues)
            {
                var data = DialogueMapper.ToData(d);
                if (data != null)
                {
                    dialogues.Add(data);
                }
            }

            var quests = new List<QuestData>();
            foreach (var q in _quests)
            {
                var data = QuestMapper.ToData(q);
                if (data != null)
                {
                    quests.Add(data);
                }
            }

            var enemies = new List<EnemyFragment>();
            foreach (var e in _enemies)
            {
                if (e != null)
                {
                    enemies.Add(new EnemyFragment(e.EnemyId.ToString(), new List<string>(e.EnemyTags)));
                }
            }

            return new FragmentLibrary(dialogues, quests, enemies);
        }

        private static int CreateSeed(DiContainer container)
        {
            var seedProvider = container.Resolve<Loot.Core.IRunSeedProvider>();
            return Loot.Core.LootSeed.Derive(seedProvider.RunSeed, SeedContextKey);
        }

        /// <summary>
        /// Falls back to loading the Demo content from <c>Resources/Narrative/*</c> when the inspector
        /// lists are empty, mirroring the project's auto-load convention so the slice works without
        /// per-scene wiring.
        /// </summary>
        private void ResolveAssetsFromResources()
        {
            if (_factKeyRegistry == null)
            {
                var registries = Resources.LoadAll<DataFactRegistry>("Narrative/Facts");
                if (registries.Length > 0)
                {
                    _factKeyRegistry = registries[0];
                }
            }

            if (IsEmpty(_archetypes))
            {
                _archetypes = new List<NpcArchetype>(Resources.LoadAll<NpcArchetype>("Narrative/Actors"));
            }

            if (IsEmpty(_dialogues))
            {
                _dialogues = new List<DialogueDefinition>(Resources.LoadAll<DialogueDefinition>("Narrative/Dialogue"));
            }

            if (IsEmpty(_quests))
            {
                _quests = new List<QuestDefinition>(Resources.LoadAll<QuestDefinition>("Narrative/Quests"));
            }

            if (IsEmpty(_enemies))
            {
                _enemies = new List<EnemyDefinition>(Resources.LoadAll<EnemyDefinition>("Narrative/Enemies"));
            }

            if (IsEmpty(_storyTemplates))
            {
                _storyTemplates = new List<StoryTemplate>(Resources.LoadAll<StoryTemplate>("Narrative/Stories"));
            }

            if (_worldContentDensityConfig == null)
            {
                _worldContentDensityConfig =
                    Resources.Load<WorldContentDensityConfig>("Narrative/WorldContentDensityConfig");
            }

            if (IsEmpty(_biomeMonsterPools))
            {
                _biomeMonsterPools = new List<BiomeMonsterPoolDefinition>(
                    Resources.LoadAll<BiomeMonsterPoolDefinition>("Combat/MonsterPools"));
            }

            if (IsEmpty(_siteDefinitions))
            {
                _siteDefinitions = new List<World.Sites.Data.SiteDefinition>(
                    Resources.LoadAll<World.Sites.Data.SiteDefinition>("World/Sites"));
            }
        }

        private static bool IsEmpty<T>(List<T> list) => list == null || list.Count == 0;
    }
}
