using System.Collections.Generic;
using CharacterProgression.Core;
using Combat.Data.Definitions;
using Core.Logging;
using Narrative;
using Narrative.Actors.Core;
using Narrative.Actors.Data;
using Narrative.Casting.Core;
using Narrative.Dialogue;
using Narrative.Dialogue.Core;
using Narrative.Dialogue.Data;
using Narrative.Director.Core;
using Narrative.Facts.Core;
using Narrative.Facts.Data;
using Narrative.Quests.Core;
using Narrative.Quests.Data;
using Narrative.Runtime.Core;
using Narrative.Runtime.Snapshots;
using Narrative.Stories.Core;
using Narrative.Stories.Data;
using UnityEngine;
using Zenject;
using DataFactRegistry = Narrative.Facts.Data.FactKeyRegistry;

namespace Core.DI
{
    /// <summary>
    /// Zenject installer for the data-driven procedural narrative system (vertical slice). Wires the
    /// unified fact store, precondition/effect machinery, casting factory, run director, dialogue
    /// runner, and save boundary from authored ScriptableObjects. Additive: the legacy
    /// <see cref="NarrativeInstaller"/> is untouched; this installer is placed in the slice scene only.
    /// </summary>
    public class NarrativeSliceInstaller : MonoInstaller
    {
        private const string SeedContextKey = "narrative-slice";

        [Header("Fact Vocabulary")]
        [SerializeField] private DataFactRegistry _factKeyRegistry;

        [Header("Fragment Library")]
        [SerializeField] private List<NpcArchetype> _archetypes = new List<NpcArchetype>();
        [SerializeField] private List<DialogueDefinition> _dialogues = new List<DialogueDefinition>();
        [SerializeField] private List<QuestDefinition> _quests = new List<QuestDefinition>();
        [SerializeField] private List<EnemyDefinition> _enemies = new List<EnemyDefinition>();

        [Header("Storylets")]
        [SerializeField] private List<StoryTemplate> _storyTemplates = new List<StoryTemplate>();

        public override void InstallBindings()
        {
            Container.Bind<IGameLogger>().To<UnityGameLogger>().AsSingle().IfNotBound();

            InstallFacts();
            InstallFragmentsAndStorylets();
            InstallDirectorAndCasting();
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
        }

        private void InstallDialogue()
        {
            Container.Bind<IStoryManager>().To<InkStoryManager>().AsSingle();
            Container.Bind<DialogueSession>().AsSingle();

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
                    ctx.Container.Resolve<IGameLogger>()))
                .AsSingle();
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
    }
}
