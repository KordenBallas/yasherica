using System.Linq;
using Combat.Core;
using Narrative.Dialogue;
using Core.Logging;
using UnityEngine;
using Zenject;

namespace Platform
{
    /// <summary>
    /// Entry adapter for a narrative platform: runs the NPC's committed encounter through the data-driven
    /// engine. Reached on demand when the player presses F in range (NpcEncounterStarter transitions the
    /// platform here — landing no longer auto-starts it). On enter it reads the platform's
    /// <see cref="NpcContent"/> and starts the <see cref="DialogueRunner"/> with the casting computed at
    /// placement (reused so the intent shown and the encounter that plays match). The bound
    /// <c>EncounterCardHandPresenter</c> drives the card-hand view. Routes the runner's outcomes back to the
    /// platform: combat → spawn enemy + CombatActiveState; a normal dialogue end (no combat) → completed.
    /// The post-combat resume + completion is owned by CombatActiveState (the runner is a singleton
    /// suspended across the combat state).
    /// </summary>
    public class DialogueActiveState : PlatformStateBase
    {
        public class Factory : PlaceholderFactory<DialogueActiveState> { }

        private readonly DialogueRunner _runner;
        private readonly IPlatformStateFactory _stateFactory;
        private readonly IGameLogger _logger;

        private IPlatform _platform;
        private NpcContent _npc;
        private bool _combatTriggered;

        [Inject]
        public DialogueActiveState(
            DialogueRunner runner,
            IPlatformStateFactory stateFactory,
            IGameLogger logger)
        {
            _runner = runner;
            _stateFactory = stateFactory;
            _logger = logger;
        }

        public override void OnEnter(IPlatform platform)
        {
            _platform = platform;
            _combatTriggered = false;
            _npc = platform.Contents.OfType<NpcContent>().FirstOrDefault();

            if (_npc?.Actor == null || _npc.Casting == null)
            {
                _logger.Warning(LogCategory.Platform, $"[DialogueActiveState] No castable encounter on platform {platform.Id}");
                TransitionToCompleted();
                return;
            }

            _runner.OnCombatTriggered += HandleCombatTriggered;
            _runner.OnDialogueEnded += HandleDialogueEnded;

            // Reuse the placement-time casting (NPC Proximity Interaction): the intent the player saw and
            // the encounter that runs are guaranteed to match, and the seeded picks are not redrawn.
            _runner.Begin(_npc.Casting);
        }

        public override void OnExit(IPlatform platform)
        {
            Unsubscribe();
            _platform = null;
            _npc = null;
        }

        private void Unsubscribe()
        {
            _runner.OnCombatTriggered -= HandleCombatTriggered;
            _runner.OnDialogueEnded -= HandleDialogueEnded;
        }

        private void HandleCombatTriggered(string enemyId, CombatInitiator initiator)
        {
            _combatTriggered = true;

            if (!int.TryParse(enemyId, out int parsedId))
            {
                _logger.Error(LogCategory.Platform, $"[DialogueActiveState] Combat triggered with non-numeric enemy id '{enemyId}' - completing.");
                TransitionToCompleted();
                return;
            }

            var enemyContent = new EnemyContent { EnemyId = parsedId, Engaged = true, Initiator = initiator };
            _platform.AddContent(enemyContent);

            // The whole platform fights as one: a camp boss's crew (pre-placed EnemyContent) joins,
            // and the latch lets a re-landing resume this battle (CombatAutoStartRule). The whole
            // platform shares the opening-round initiator (D2) so the read-out is consistent.
            foreach (var content in _platform.Contents)
            {
                if (content is EnemyContent enemy)
                {
                    enemy.Engaged = true;
                    enemy.Initiator = initiator;
                }
            }

            _npc?.DestroyNpcVisual();
            TransitionToCombat();
        }

        private void HandleDialogueEnded(string outcome)
        {
            if (_combatTriggered)
            {
                return; // CombatActiveState resumes the runner and completes the platform after combat.
            }

            TransitionToCompleted();
        }

        private void TransitionToCombat()
        {
            if (_platform == null)
            {
                return;
            }

            var combatState = _stateFactory.CreateActiveState(_platform, ContentType.Enemy);
            _platform.TransitionToState(combatState);
        }

        private void TransitionToCompleted()
        {
            if (_platform == null)
            {
                return;
            }

            var completedState = _stateFactory.CreateCompletedState();
            _platform.TransitionToState(completedState);
        }
    }
}
