using System;
using System.Collections.Generic;
using Core.Logging;
using Narrative.Dialogue.Core;
using Narrative.Facts.Core;
using Narrative.Quests.Core;
using CharacterProgression.Core;
using CastingModel = global::Narrative.Casting.Core.Casting;

namespace Narrative.Dialogue
{
    /// <summary>
    /// Drives one <see cref="DialogueSession"/> per casting and is the tag bridge to game systems
    /// (R4). Tags are the only Ink→C# channel: <c>fact:</c> writes facts (footprint-gated against the
    /// dialogue's own declared writes), <c>offer-quest:</c>/<c>start-combat:</c> dispatch to the quest
    /// and combat systems, <c>speaker:</c>/<c>outcome:</c> drive presentation/termination.
    ///
    /// Suspension state machine (B1): a <c>start-combat:</c> transitions to
    /// <see cref="DialogueRunnerState.AwaitingExternal"/> and stops pumping Ink until
    /// <see cref="ReportCombatResult"/> sets the write-back var and <see cref="Resume"/> performs exactly
    /// one continue. Empty optional slots fail closed (W2-2). UnityEngine-free; a thin view adapter
    /// subscribes to its events.
    /// </summary>
    public sealed class DialogueRunner
    {
        public const string CombatWonVariable = "combat_won";
        public const string QuestAcceptedVariable = "quest_accepted";

        private readonly DialogueSession _session;
        private readonly IFactStore _store;
        private readonly IFactEffectApplier _applier;
        private readonly DialogueTagParser _parser;
        private readonly IRunProgressionRecorder _recorder;
        private readonly IGameLogger _logger;

        private CastingModel _casting;
        private QuestInstance _activeQuest;

        public DialogueRunnerState State { get; private set; } = DialogueRunnerState.Ended;

        public event Action<string> OnSpeakerChanged;
        public event Action<string> OnLine;
        public event Action<IReadOnlyList<StoryChoice>> OnChoices;
        public event Action<string> OnCombatTriggered;
        public event Action<string> OnQuestStarted;
        public event Action<string> OnDialogueEnded;

        public DialogueRunner(DialogueSession session, IFactStore store, IFactEffectApplier applier,
            DialogueTagParser parser, IRunProgressionRecorder recorder = null, IGameLogger logger = null)
        {
            _session = session;
            _store = store;
            _applier = applier;
            _parser = parser;
            _recorder = recorder;
            _logger = logger;
        }

        public QuestInstance ActiveQuest => _activeQuest;

        /// <summary>Begins a fresh conversation for a casting and pumps to the first stop.</summary>
        public void Begin(CastingModel casting)
        {
            _casting = casting;
            _activeQuest = null;
            State = DialogueRunnerState.Running;
            if (casting?.Actor != null)
            {
                _recorder?.RecordNpcEncounter(casting.Actor.ArchetypeId);
            }

            _session.StartFresh(casting.Dialogue, casting.Context);
            Pump();
        }

        public void SelectChoice(int choiceIndex)
        {
            if (State != DialogueRunnerState.Running || !_session.HasChoices)
            {
                return;
            }

            _session.Choose(choiceIndex);
            Pump();
        }

        /// <summary>
        /// Advances past a gated line (the player pressed continue). No-op unless the runner is parked in
        /// <see cref="DialogueRunnerState.AwaitingContinue"/>; the line-gate counterpart of <see cref="Resume"/>.
        /// </summary>
        public void Continue()
        {
            if (State != DialogueRunnerState.AwaitingContinue)
            {
                return; // not gated on a line; ignore
            }

            State = DialogueRunnerState.Running;
            Pump();
        }

        /// <summary>Reports the combat outcome, sets the write-back var, and resumes (B1).</summary>
        public void ReportCombatResult(bool won)
        {
            if (State != DialogueRunnerState.AwaitingExternal)
            {
                _logger?.Warning("[DialogueRunner] ReportCombatResult with no pending combat - ignored.");
                return;
            }

            _session.SetVariable(CombatWonVariable, won);
            Resume();
        }

        private void Resume()
        {
            if (State != DialogueRunnerState.AwaitingExternal)
            {
                return; // double-resume guard
            }

            State = DialogueRunnerState.Running;
            Pump();
        }

        private void Pump()
        {
            while (State == DialogueRunnerState.Running && _session.CanContinue)
            {
                _session.Continue();
                if (!ProcessTags(_session.CurrentTags))
                {
                    return; // a tag suspended or ended the conversation
                }

                if (!string.IsNullOrEmpty(_session.CurrentText))
                {
                    OnLine?.Invoke(_session.CurrentText);
                    State = DialogueRunnerState.AwaitingContinue; // gate: one readable line at a time
                    return;
                }
            }

            if (State != DialogueRunnerState.Running)
            {
                return;
            }

            if (_session.HasChoices)
            {
                OnChoices?.Invoke(_session.CurrentChoices);
            }
            else if (!_session.CanContinue)
            {
                End("exit");
            }
        }

        /// <summary>Returns false if a tag suspended or ended the conversation (stop pumping).</summary>
        private bool ProcessTags(IReadOnlyList<string> tags)
        {
            if (tags == null)
            {
                return true;
            }

            for (int i = 0; i < tags.Count; i++)
            {
                var tag = _parser.Parse(tags[i]);
                switch (tag.Kind)
                {
                    case DialogueTagKind.Speaker:
                        OnSpeakerChanged?.Invoke(tag.Argument);
                        break;
                    case DialogueTagKind.Fact:
                        _applier.Apply(tag.Effect, _store, _casting?.Context, _session.Footprint);
                        break;
                    case DialogueTagKind.OfferQuest:
                        HandleOfferQuest();
                        break;
                    case DialogueTagKind.StartCombat:
                        return HandleStartCombat(); // may suspend (returns false)
                    case DialogueTagKind.Outcome:
                        End(tag.Argument);
                        return false;
                }
            }

            return true;
        }

        private void HandleOfferQuest()
        {
            if (_casting == null || !_casting.QuestSlotFilled)
            {
                _logger?.Warning("[DialogueRunner] offer-quest with no quest in casting - failing closed.");
                _session.SetVariable(QuestAcceptedVariable, false);
                return;
            }

            _activeQuest = new QuestInstance(_casting.OptionalQuest, _recorder);
            _activeQuest.Start();
            _session.SetVariable(QuestAcceptedVariable, true);
            OnQuestStarted?.Invoke(_casting.OptionalQuest.QuestId);
        }

        private bool HandleStartCombat()
        {
            if (_casting == null || !_casting.CombatAllowed)
            {
                // W2-2: empty/unavailable combat slot - no transition; set a safe write-back so Ink continues.
                _logger?.Warning("[DialogueRunner] start-combat with no combat in casting - failing closed.");
                _session.SetVariable(CombatWonVariable, false);
                return true; // keep pumping
            }

            State = DialogueRunnerState.AwaitingExternal;
            OnCombatTriggered?.Invoke(_casting.OptionalEnemyId);
            return false; // suspend; trailing tags in this step are ignored (B1)
        }

        private void End(string outcome)
        {
            State = DialogueRunnerState.Ended;
            OnDialogueEnded?.Invoke(outcome);
        }
    }
}
