using System;
using System.Collections.Generic;
using Narrative;
using Narrative.Data.Definitions;
using Narrative.Dialogue;
using Narrative.Generation;
using Narrative.View;
using UnityEngine;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Zenject installer for the simplified narrative system.
    /// Binds definition pools, dual story managers, composite dialogue, and generation.
    /// </summary>
    public class NarrativeInstaller : MonoInstaller
    {
        [Header("Story Definitions")]
        [SerializeField] private List<StoryDefinition> _storyDefinitions;

        [Header("NPC Definitions")]
        [SerializeField] private List<NpcDefinition> _npcDefinitions;

        [Header("Reward Definitions")]
        [SerializeField] private List<RewardDefinition> _rewardDefinitions;

        [Header("Level Configuration")]
        [SerializeField] private LevelNarrativeConfig _levelConfig;

        [Header("Dialogue UI")]
        [SerializeField] private DialogueView _dialogueView;
        [SerializeField] private DialogueView _dialogueViewPrefab;

        public override void InstallBindings()
        {
            Debug.Log("[NarrativeInstaller] Installing simplified narrative system");

            InstallPools();
            InstallGeneration();
            InstallStoryManagers();
            InstallDialogueView();
            InstallDialoguePresenter();
            InstallExternalFunctionBinder();
        }

        private void InstallPools()
        {
            var stories = _storyDefinitions != null
                ? _storyDefinitions as IReadOnlyList<StoryDefinition>
                : new List<StoryDefinition>() as IReadOnlyList<StoryDefinition>;

            var npcs = _npcDefinitions != null
                ? _npcDefinitions as IReadOnlyList<NpcDefinition>
                : new List<NpcDefinition>() as IReadOnlyList<NpcDefinition>;

            Container.Bind<IStoryPool>()
                .To<StoryPool>()
                .AsSingle()
                .WithArguments(stories);

            Container.Bind<INpcPool>()
                .To<NpcPool>()
                .AsSingle()
                .WithArguments(npcs);

            // Seeded from the run seed so reward slot rolls are reproducible per run.
            Container.Bind<IRewardResolver>()
                .To<RewardResolver>()
                .FromMethod(ctx => new RewardResolver(CreateSeededRandom(ctx.Container, "narrative-rewards")))
                .AsSingle();

            // Always bind LevelNarrativeConfig - create default if not assigned
            if (_levelConfig == null)
            {
                Debug.LogWarning("[NarrativeInstaller] No LevelNarrativeConfig assigned. Creating default instance.");
                _levelConfig = ScriptableObject.CreateInstance<LevelNarrativeConfig>();
            }

            Container.Bind<LevelNarrativeConfig>()
                .FromInstance(_levelConfig)
                .AsSingle();
        }

        private void InstallGeneration()
        {
            Container.Bind<ILevelNarrativeGenerator>()
                .To<LevelNarrativeGenerator>()
                .FromMethod(ctx => new LevelNarrativeGenerator(
                    ctx.Container.Resolve<IStoryPool>(),
                    ctx.Container.Resolve<INpcPool>(),
                    ctx.Container.Resolve<IRewardResolver>(),
                    CreateSeededRandom(ctx.Container, "narrative-generation")))
                .AsSingle();
        }

        private static System.Random CreateSeededRandom(DiContainer container, string contextKey)
        {
            var seedProvider = container.Resolve<Loot.Core.IRunSeedProvider>();
            return new System.Random(Loot.Core.LootSeed.Derive(seedProvider.RunSeed, contextKey));
        }

        private void InstallStoryManagers()
        {
            // Two IStoryManager instances: one for quest stories, one for NPC character Ink.
            // AsCached (not AsSingle) because Zenject 6+ disallows multiple AsSingle for the same concrete type.
            Container.Bind<IStoryManager>()
                .WithId("story")
                .To<InkStoryManager>()
                .AsCached();

            Container.Bind<IStoryManager>()
                .WithId("npc")
                .To<InkStoryManager>()
                .AsCached();
        }

        private void InstallDialogueView()
        {
            if (_dialogueView != null)
            {
                Container.Bind<IDialogueView>()
                    .FromInstance(_dialogueView)
                    .AsSingle();
            }
            else if (_dialogueViewPrefab != null)
            {
                Container.Bind<IDialogueView>()
                    .To<DialogueView>()
                    .FromComponentInNewPrefab(_dialogueViewPrefab)
                    .AsSingle()
                    .NonLazy();
            }
            else
            {
                throw new System.InvalidOperationException(
                    "[NarrativeInstaller] No dialogue view assigned. " +
                    "Assign either _dialogueView or _dialogueViewPrefab.");
            }
        }

        private void InstallDialoguePresenter()
        {
            // Bind both IDialoguePresenter and IDisposable to the same singleton so
            // Zenject automatically calls Dispose() on container teardown.
            Container.Bind(typeof(IDialoguePresenter), typeof(IDisposable))
                .To<CompositeDialoguePresenter>()
                .FromMethod(ctx =>
                {
                    var storyMgr = ctx.Container.ResolveId<IStoryManager>("story");
                    var npcMgr = ctx.Container.ResolveId<IStoryManager>("npc");
                    var binder = ctx.Container.Resolve<IInkExternalFunctionBinder>();
                    var view = ctx.Container.Resolve<IDialogueView>();
                    return new CompositeDialoguePresenter(storyMgr, npcMgr, binder, view);
                })
                .AsSingle();
        }

        private void InstallExternalFunctionBinder()
        {
            Container.Bind<IInkExternalFunctionBinder>()
                .To<InkExternalFunctionBinder>()
                .AsSingle();
        }
    }
}
