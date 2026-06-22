using CharacterSystem.Runtime;
using Narrative.Actors.Core;
using Narrative.Actors.Data;
using Narrative.Stories.Core;
using UnityEngine;

namespace Platform
{
    /// <summary>
    /// Platform content for a planner-placed NPC: carries the run-stable <see cref="NpcInstance"/>, its
    /// archetype (the visual source), and the committed story the entry adapter runs. Spawns the modular
    /// character body on initialize.
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

        /// <summary>Portrait for the dialogue view, from the archetype.</summary>
        public Sprite Portrait => Archetype != null ? Archetype.Portrait : null;

        /// <summary>The spawned NPC GameObject (modular character), if any.</summary>
        public GameObject NpcVisual { get; private set; }

        private readonly IModularCharacterFactory _modularFactory;

        public NpcContent(NpcArchetype archetype, NpcInstance actor, StoryTemplateData plannedStory,
            IModularCharacterFactory modularFactory)
        {
            Archetype = archetype;
            Actor = actor;
            PlannedStory = plannedStory;
            _modularFactory = modularFactory;
        }

        public override void Initialize(IPlatform platform)
        {
            if (Actor == null)
            {
                Debug.LogWarning($"[NpcContent] No actor on platform {platform.Id}");
                return;
            }

            SpawnModularVisual(platform);
        }

        private void SpawnModularVisual(IPlatform platform)
        {
            if (_modularFactory == null || Archetype?.Assembly == null)
            {
                Debug.LogWarning($"[NpcContent] Actor '{Actor?.InstanceId}' has no modular factory/assembly - no visual.");
                return;
            }

            var character = _modularFactory.Create(Archetype.Assembly, null);
            if (character == null)
            {
                return;
            }

            NpcVisual = character.gameObject;
            NpcVisual.transform.position = platform.Visual?.Position ?? Vector3.zero;
            NpcVisual.name = $"NPC_{Actor.InstanceId}";
        }

        public void DestroyNpcVisual()
        {
            if (NpcVisual != null)
            {
                UnityEngine.Object.Destroy(NpcVisual);
                NpcVisual = null;
            }
        }
    }
}
