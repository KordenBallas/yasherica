using CharacterSystem.Runtime;
using Narrative.Actors.Core;
using Narrative.Actors.Data;
using Narrative.Casting.Core;
using Narrative.Interaction;
using Narrative.Interaction.Core;
using Narrative.Stories.Core;
using Core.Logging;
using UnityEngine;

namespace Platform
{
    /// <summary>
    /// Platform content for a planner-placed NPC: carries the run-stable <see cref="NpcInstance"/>, its
    /// archetype (the visual source), the committed story, and the placement-time <see cref="Casting"/> +
    /// <see cref="NpcIntent"/> the proximity system reads. Spawns the modular character body on initialize
    /// and binds itself into the interaction system (overhead view + registry handle).
    /// </summary>
    public class NpcContent : PlatformContentBase
    {
        public override ContentType Type => ContentType.Npc;

        /// <summary>The run-stable actor minted by the planner for this platform (R12).</summary>
        public NpcInstance Actor { get; }

        /// <summary>The archetype SO backing the actor — source of the visual assembly and portrait.</summary>
        public NpcArchetype Archetype { get; }

        /// <summary>The story the planner committed for this encounter; run by the entry adapter.</summary>
        public StoryTemplateData PlannedStory { get; }

        /// <summary>
        /// The casting computed once at placement against the live facts, reused by the encounter so the
        /// seeded picks stay deterministic and the shown intent matches what actually plays. May be null
        /// if the story could not be cast.
        /// </summary>
        public Casting Casting { get; }

        /// <summary>The intent derived from <see cref="Casting"/> at placement (quest-bearer / hostile / plain).</summary>
        public NpcIntent Intent { get; }

        /// <summary>
        /// True when this NPC is a camp boss fronting a crew of pre-placed enemies: he gets the larger
        /// engagement radius and gates the platform's combat (no fight until he is engaged).
        /// </summary>
        public bool IsCampBoss { get; }

        /// <summary>
        /// Latched by <see cref="Narrative.Interaction.NpcEncounterStarter"/> once the player engages
        /// this NPC (talk or aggro). A camp platform holds its fight until the boss's encounter starts.
        /// </summary>
        public bool EncounterStarted { get; set; }

        /// <summary>The platform that owns this NPC's encounter; set on initialize.</summary>
        public IPlatform OwningPlatform { get; private set; }

        /// <summary>Portrait for the dialogue view, from the archetype.</summary>
        public Sprite Portrait => Archetype != null ? Archetype.Portrait : null;

        /// <summary>The spawned NPC GameObject (modular character), if any.</summary>
        public GameObject NpcVisual { get; private set; }

        private readonly IModularCharacterFactory _modularFactory;
        private readonly INpcInteractionService _interactionService;
        private readonly IDemoRoleTintApplier _tintApplier;

        public NpcContent(NpcArchetype archetype, NpcInstance actor, StoryTemplateData plannedStory,
            Casting casting, NpcIntent intent, IModularCharacterFactory modularFactory,
            INpcInteractionService interactionService, IDemoRoleTintApplier tintApplier = null,
            bool isCampBoss = false)
        {
            Archetype = archetype;
            Actor = actor;
            PlannedStory = plannedStory;
            Casting = casting;
            Intent = intent;
            IsCampBoss = isCampBoss;
            _modularFactory = modularFactory;
            _interactionService = interactionService;
            _tintApplier = tintApplier;
        }

        public override void Initialize(IPlatform platform)
        {
            if (Actor == null)
            {
                Logger?.Warning(LogCategory.Platform,$"[NpcContent] No actor on platform {platform.Id}");
                return;
            }

            OwningPlatform = platform;
            SpawnModularVisual(platform);
            _interactionService?.Bind(this);
        }

        private void SpawnModularVisual(IPlatform platform)
        {
            if (_modularFactory == null || Archetype?.Assembly == null)
            {
                Logger?.Warning(LogCategory.Platform,$"[NpcContent] Actor '{Actor?.InstanceId}' has no modular factory/assembly - no visual.");
                return;
            }

            var character = _modularFactory.Create(Archetype.Assembly, null);
            if (character == null)
            {
                return;
            }

            // Host the rig so the NPC root's +Z is the model's face (the placeholder art
            // faces -Z) — anything orienting the NPC root then reads face-first.
            NpcVisual = CharacterRigHost.Wrap(
                character, $"NPC_{Actor.InstanceId}", platform.Visual?.Position ?? Vector3.zero);
            _tintApplier?.Apply(NpcVisual, Archetype.DemoTint);
        }

        public void DestroyNpcVisual()
        {
            _interactionService?.Unbind(this);

            if (NpcVisual != null)
            {
                UnityEngine.Object.Destroy(NpcVisual);
                NpcVisual = null;
            }
        }
    }
}
