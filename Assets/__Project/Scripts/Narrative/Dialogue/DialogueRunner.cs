using System;
using System.Collections.Generic;
using Combat.Core;
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

        /// <summary>The outcome <see cref="Leave"/> reports — walking away resolves the *story* (it is
        /// never re-offered) but does not advance its *thread* (a browsed-and-abandoned errand still
        /// lapses, FR4). The resolution relay keys off this value.</summary>
        public const string LeaveOutcome = "leave";

        private readonly DialogueSession _session;
        private readonly IFactStore _store;
        private readonly IFactEffectApplier _applier;
        private readonly DialogueTagParser _parser;
        private readonly IRunProgressionRecorder _recorder;
        private readonly ILiveQuestRegistry _questRegistry;
        private readonly IGameLogger _logger;

        private CastingModel _casting;
        private QuestInstance _activeQuest;

        // One-shot latch (D2): set when the player picks an Attack choice, so the next start-combat this
        // choice's Ink branch fires is classified as player-initiated (the player chose to attack) rather
        // than enemy-initiated (an NPC that turned hostile on its own). Consumed at the combat trigger and
        // cleared per encounter in Begin.
        private bool _pendingPlayerCombat;

        public DialogueRunnerState State { get; private set; } = DialogueRunnerState.Ended;

        public event Action<string> OnSpeakerChanged;
        public event Action<string> OnLine;
        public event Action<IReadOnlyList<StoryChoice>> OnChoices;

        /// <summary>
        /// Raised when the encounter routes into combat, carrying the enemy id and <b>who initiated</b>
        /// the fight (D2): <see cref="CombatInitiator.Player"/> from the Attack card (the player chose to
        /// attack), <see cref="CombatInitiator.Enemy"/> from a <c>start-combat:</c> tag (the NPC turned
        /// hostile). The initiator decides who leads the opening round.
        /// </summary>
        public event Action<string, CombatInitiator> OnCombatTriggered;

        /// <summary>
        /// Raised when a dialogue-routed fight reports its result (P1-7), BEFORE Ink resumes — true =
        /// the player won (the NPC died). The Monster-verb consequence relay folds the kill into
        /// facts/threads off this event; it fires for both entry points (Attack card and NPC
        /// self-initiation) so the verb has one consequence path.
        /// </summary>
        public event Action<bool> OnCombatResolved;
        public event Action<string> OnQuestStarted;
        public event Action<string> OnQuestCompleted;
        public event Action<string> OnQuestFailed;
        public event Action<string> OnDialogueEnded;

        public DialogueRunner(DialogueSession session, IFactStore store, IFactEffectApplier applier,
            DialogueTagParser parser, IRunProgressionRecorder recorder = null,
            ILiveQuestRegistry questRegistry = null, IGameLogger logger = null)
        {
            _session = session;
            _store = store;
            _applier = applier;
            _parser = parser;
            _recorder = recorder;
            _questRegistry = questRegistry;
            _logger = logger;
        }

        public QuestInstance ActiveQuest => _activeQuest;

        /// <summary>
        /// The quest this casting can offer (its first filled quest slot), or null. Read by the encounter
        /// UI to label the quest card with the job's title + summary BEFORE the player accepts — the
        /// offer-quest tag (which mints <see cref="ActiveQuest"/>) only fires once the offer choice is taken.
        /// </summary>
        public QuestData OfferedQuest => _casting?.OptionalQuest;

        /// <summary>Every quest the casting can offer (P1-9: several resolutions of one trouble).</summary>
        public IReadOnlyList<QuestData> OfferedQuests =>
            _casting?.Quests ?? (IReadOnlyList<QuestData>)Array.Empty<QuestData>();

        /// <summary>
        /// Resolves an offered quest by the <c>offer-quest: &lt;tag&gt;</c> vocabulary (P1-9) — the same
        /// tag an Ink choice carries to label its card and its branch fires to mint the instance. An
        /// empty tag resolves the single/first offer (legacy single-offer authoring).
        /// </summary>
        public QuestData OfferedQuestByTag(string questTag) => _casting?.QuestByTag(questTag);

        /// <summary>Archetype id of the encounter's NPC — the UI resolves its portrait from this.</summary>
        public string EncounterArchetypeId => _casting?.Actor?.ArchetypeId;

        /// <summary>Run-stable instance id of the encounter's NPC (the per-actor fact subject).</summary>
        public string EncounterActorId => _casting?.Actor?.InstanceId;

        /// <summary>The encounter NPC's chosen display name — the UI's default speaker label.</summary>
        public string EncounterDisplayName => _casting?.Actor?.ChosenDisplayName;

        /// <summary>
        /// Whether the active casting carries a combat slot — i.e. the encounter card-hand may present an
        /// Attack card (the Monster verb). Mirrors the gate <see cref="HandleStartCombat"/> checks.
        /// </summary>
        public bool CombatAvailable => _casting?.CombatAllowed == true;

        /// <summary>The enemy id the Attack card / <see cref="TriggerCombat"/> initiates (null when none).</summary>
        public string CombatEnemyId => _casting?.OptionalEnemyId;

        /// <summary>The story the active casting presents (empty when cast outside a story) — read by
        /// the resolution relay when <see cref="OnDialogueEnded"/> fires.</summary>
        public string ActiveStoryId => _casting?.StoryId ?? string.Empty;

        /// <summary>The active story's thread label (empty for threadless stories).</summary>
        public string ActiveThreadId => _casting?.ThreadId ?? string.Empty;

        /// <summary>Begins a fresh conversation for a casting and pumps to the first stop.</summary>
        public void Begin(CastingModel casting)
        {
            _casting = casting;
            // Restore the in-flight instance if this casting carries a quest already offered this run
            // (cross-dialogue continuity); otherwise a fresh offer-quest tag mints and registers it.
            // With several offers (P1-9) at most one can have been taken - the first registered wins.
            _activeQuest = FindRegisteredQuest(casting);
            State = DialogueRunnerState.Running;
            _pendingPlayerCombat = false;
            if (casting?.Actor != null)
            {
                _recorder?.RecordNpcEncounter(casting.Actor.ArchetypeId);
            }

            _session.StartFresh(casting.Dialogue, casting.Context);
            Pump();
        }

        /// <summary>
        /// Selects an Ink choice. <paramref name="playerInitiatesCombat"/> marks a player-chosen Attack
        /// choice (D2): if the picked branch fires <c>start-combat:</c>, that fight is attributed to the
        /// player, not the enemy — the encounter card-hand passes true for its Attack card.
        /// </summary>
        public void SelectChoice(int choiceIndex, bool playerInitiatesCombat = false)
        {
            if (State != DialogueRunnerState.Running || !_session.HasChoices)
            {
                return;
            }

            if (playerInitiatesCombat)
            {
                _pendingPlayerCombat = true;
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
                _logger?.Warning(LogCategory.Dialogue,"[DialogueRunner] ReportCombatResult with no pending combat - ignored.");
                return;
            }

            _session.SetVariable(CombatWonVariable, won);
            // Consequences fold in before Ink resumes, so a post-combat branch already sees the
            // written facts (e.g. a reply gated on the actor being slain).
            OnCombatResolved?.Invoke(won);
            Resume();
        }

        /// <summary>
        /// Player-initiated combat from the encounter card-hand's Attack card (the Monster verb). Suspends
        /// to <see cref="DialogueRunnerState.AwaitingExternal"/> and fires <see cref="OnCombatTriggered"/>
        /// exactly like a <c>start-combat:</c> tag, so the existing <see cref="ReportCombatResult"/> resume
        /// and platform combat routing are reused. No-op (warns) when no combat is available or while ended
        /// or already suspended.
        /// </summary>
        public void TriggerCombat()
        {
            if (State == DialogueRunnerState.Ended || State == DialogueRunnerState.AwaitingExternal)
            {
                _logger?.Warning(LogCategory.Dialogue,"[DialogueRunner] TriggerCombat in a non-interactive state - ignored.");
                return;
            }

            if (!CombatAvailable)
            {
                _logger?.Warning(LogCategory.Dialogue,"[DialogueRunner] TriggerCombat with no combat in casting - ignored.");
                return;
            }

            State = DialogueRunnerState.AwaitingExternal;
            // The player chose to attack — the player leads the opening round.
            OnCombatTriggered?.Invoke(_casting.OptionalEnemyId, CombatInitiator.Player);
        }

        /// <summary>
        /// Player-initiated end of the encounter from the card-hand's Leave card. Ends the conversation
        /// gracefully (outcome <c>"leave"</c>). No-op when already ended; refuses to abort while suspended
        /// on combat (a full mid-suspension abort is a deferred follow-up).
        /// </summary>
        public void Leave()
        {
            if (State == DialogueRunnerState.Ended)
            {
                return;
            }

            if (State == DialogueRunnerState.AwaitingExternal)
            {
                _logger?.Warning(LogCategory.Dialogue,"[DialogueRunner] Leave while suspended on combat - ignored.");
                return;
            }

            End(LeaveOutcome);
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
                        HandleOfferQuest(tag.Argument);
                        break;
                    case DialogueTagKind.AdvanceObjective:
                        HandleAdvanceObjective(tag.Argument);
                        break;
                    case DialogueTagKind.CompleteQuest:
                        HandleCompleteQuest(tag.Argument);
                        break;
                    case DialogueTagKind.FailQuest:
                        HandleFailQuest();
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

        private void HandleOfferQuest(string questTag)
        {
            var quest = _casting?.QuestByTag(questTag);
            if (quest == null)
            {
                _logger?.Warning(LogCategory.Dialogue,$"[DialogueRunner] offer-quest '{questTag}' with no matching quest in casting - failing closed.");
                _session.SetVariable(QuestAcceptedVariable, false);
                return;
            }

            // A restored instance (offered on an earlier platform) is reused as-is — no second Start().
            // With several offers (P1-9) the picked branch decides which quest is minted; the others
            // stay untaken (they were alternative resolutions, not extra rewards).
            if (_activeQuest == null || _activeQuest.Data.QuestId != quest.QuestId)
            {
                if (_questRegistry != null && _questRegistry.TryGet(quest.QuestId, out var live))
                {
                    _activeQuest = live;
                }
                else
                {
                    _activeQuest = new QuestInstance(quest, _recorder);
                    // The offer's origin (giver + thread) rides the instance for the quest log's
                    // saga grouping; presentation-only, never gating.
                    _activeQuest.SetOrigin(_casting?.Actor?.ChosenDisplayName, _casting?.ThreadId);
                    _activeQuest.Start();
                    _questRegistry?.Register(_activeQuest);
                }
            }

            _session.SetVariable(QuestAcceptedVariable, true);
            OnQuestStarted?.Invoke(quest.QuestId);
        }

        /// <summary>The first of the casting's quests already registered this run, or null.</summary>
        private QuestInstance FindRegisteredQuest(CastingModel casting)
        {
            if (casting == null || _questRegistry == null)
            {
                return null;
            }

            var quests = casting.Quests;
            for (int i = 0; i < quests.Count; i++)
            {
                if (_questRegistry.TryGet(quests[i].QuestId, out var live))
                {
                    return live;
                }
            }

            return null;
        }

        /// <summary>Advances an objective of the active quest and applies any objective-completion effects.</summary>
        private void HandleAdvanceObjective(string argument)
        {
            if (_activeQuest == null)
            {
                _logger?.Warning(LogCategory.Dialogue,"[DialogueRunner] advance-objective with no active quest - ignored.");
                return;
            }

            var parts = (argument ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                _logger?.Warning(LogCategory.Dialogue,"[DialogueRunner] advance-objective with no objective id - ignored.");
                return;
            }

            int amount = parts.Length >= 2 && int.TryParse(parts[1], out var parsed) ? parsed : 1;
            ApplyQuestEffects(_activeQuest.AdvanceObjective(parts[0], amount));
        }

        /// <summary>
        /// Completes a quest, applies its on-complete effects, and records the transition. Resolves the
        /// target by the tag's quest id from the live registry when the current casting does not carry it,
        /// so an encounter can close a quest offered elsewhere — e.g. defeating the raider closes the
        /// bounty the player took from the farmer, without the raid story carrying a quest slot.
        /// </summary>
        private void HandleCompleteQuest(string questId)
        {
            var quest = ResolveQuestForCompletion(questId);
            if (quest == null)
            {
                _logger?.Warning(LogCategory.Dialogue,$"[DialogueRunner] complete-quest '{questId}' with no matching active/registered quest - ignored.");
                return;
            }

            _activeQuest = quest; // so the platform-completion reward granter sees the completed quest
            ApplyQuestEffects(quest.Complete());
            OnQuestCompleted?.Invoke(quest.Data.QuestId);
        }

        /// <summary>
        /// The active quest when it matches the tag (or the tag gave no id); otherwise the live registry's
        /// instance for the id; otherwise null.
        /// </summary>
        private QuestInstance ResolveQuestForCompletion(string questId)
        {
            if (_activeQuest != null && (string.IsNullOrEmpty(questId) || _activeQuest.Data.QuestId == questId))
            {
                return _activeQuest;
            }

            if (!string.IsNullOrEmpty(questId) && _questRegistry != null && _questRegistry.TryGet(questId, out var live))
            {
                return live;
            }

            return _activeQuest;
        }

        /// <summary>Fails the active quest, applies its on-fail effects, and records the transition.</summary>
        private void HandleFailQuest()
        {
            if (_activeQuest == null)
            {
                _logger?.Warning(LogCategory.Dialogue,"[DialogueRunner] fail-quest with no active quest - ignored.");
                return;
            }

            ApplyQuestEffects(_activeQuest.Fail());
            OnQuestFailed?.Invoke(_activeQuest.Data.QuestId);
        }

        /// <summary>
        /// Applies quest-emitted fact effects gated against the quest's OWN footprint (W2-1), not the
        /// dialogue session's — a quest is validated and applied against the shapes it declared.
        /// </summary>
        private void ApplyQuestEffects(IReadOnlyList<FactEffectCore> effects)
        {
            if (effects == null)
            {
                return;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                _applier.Apply(effects[i], _store, _casting?.Context, _activeQuest.Footprint);
            }
        }

        private bool HandleStartCombat()
        {
            if (_casting == null || !_casting.CombatAllowed)
            {
                // W2-2: empty/unavailable combat slot - no transition; set a safe write-back so Ink continues.
                _logger?.Warning(LogCategory.Dialogue,"[DialogueRunner] start-combat with no combat in casting - failing closed.");
                _session.SetVariable(CombatWonVariable, false);
                return true; // keep pumping
            }

            State = DialogueRunnerState.AwaitingExternal;
            // A player-chosen Attack choice leads with the player; a start-combat from any other branch
            // means the NPC turned hostile on its own, so the enemy leads (D2). The latch is one-shot.
            var initiator = _pendingPlayerCombat ? CombatInitiator.Player : CombatInitiator.Enemy;
            _pendingPlayerCombat = false;
            OnCombatTriggered?.Invoke(_casting.OptionalEnemyId, initiator);
            return false; // suspend; trailing tags in this step are ignored (B1)
        }

        private void End(string outcome)
        {
            State = DialogueRunnerState.Ended;
            OnDialogueEnded?.Invoke(outcome);
        }
    }
}
