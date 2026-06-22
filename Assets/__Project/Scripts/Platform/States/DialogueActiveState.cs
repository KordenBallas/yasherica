using System.Linq;
using Narrative.Director.Core;
using Narrative.Dialogue;
using UnityEngine;
using Zenject;

namespace Platform
{
    /// <summary>
    /// Entry adapter for a narrative platform: runs the planner's committed encounter through the
    /// data-driven engine. On enter it reads the platform's <see cref="NpcContent"/> (its minted actor +
    /// planned story) and asks <see cref="EncounterDirector.BeginPlanned"/> to cast and start the
    /// <see cref="DialogueRunner"/>; the bound <c>DialogueRunnerViewPresenter</c> drives the view. Routes
    /// the runner's outcomes back to the platform: combat → spawn enemy + CombatActiveState; a normal
    /// dialogue end (no combat) → completed. The post-combat resume + completion is owned by
    /// CombatActiveState (the runner is a singleton suspended across the combat state).
    /// </summary>
    public class DialogueActiveState : PlatformStateBase
    {
        public class Factory : PlaceholderFactory<DialogueActiveState> { }

        private readonly EncounterDirector _encounterDirector;
        private readonly DialogueRunner _runner;
        private readonly IDialogueView _view;
        private readonly IPlatformStateFactory _stateFactory;

        private IPlatform _platform;
        private NpcContent _npc;
        private bool _combatTriggered;

        [Inject]
        public DialogueActiveState(
            EncounterDirector encounterDirector,
            DialogueRunner runner,
            IDialogueView view,
            IPlatformStateFactory stateFactory)
        {
            _encounterDirector = encounterDirector;
            _runner = runner;
            _view = view;
            _stateFactory = stateFactory;
        }

        public override void OnEnter(IPlatform platform)
        {
            _platform = platform;
            _combatTriggered = false;
            _npc = platform.Contents.OfType<NpcContent>().FirstOrDefault();

            if (_npc?.Actor == null || _npc.PlannedStory == null)
            {
                Debug.LogWarning($"[DialogueActiveState] No planned encounter on platform {platform.Id}");
                TransitionToCompleted();
                return;
            }

            _runner.OnCombatTriggered += HandleCombatTriggered;
            _runner.OnDialogueEnded += HandleDialogueEnded;

            if (_npc.Portrait != null)
            {
                _view.SetPortrait(_npc.Portrait);
            }
            else
            {
                _view.ClearPortrait();
            }

            if (!_encounterDirector.BeginPlanned(_npc.PlannedStory, _npc.Actor))
            {
                Debug.LogWarning($"[DialogueActiveState] Encounter could not start (story '{_npc.PlannedStory.StoryId}') on platform {platform.Id}");
                Unsubscribe();
                TransitionToCompleted();
            }
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

        private void HandleCombatTriggered(string enemyId)
        {
            _combatTriggered = true;

            if (!int.TryParse(enemyId, out int parsedId))
            {
                Debug.LogError($"[DialogueActiveState] Combat triggered with non-numeric enemy id '{enemyId}' - completing.");
                TransitionToCompleted();
                return;
            }

            var enemyContent = new EnemyContent { EnemyId = parsedId };
            _platform.AddContent(enemyContent);
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
