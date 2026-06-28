using System;
using System.Collections.Generic;
using Core.Logging;
using Narrative.Dialogue;
using Zenject;

namespace Narrative.Encounter
{
    /// <summary>
    /// MVP presenter bridging the pure-C# <see cref="DialogueRunner"/> to the encounter card-hand view.
    /// It replaces <c>DialogueRunnerViewPresenter</c> as the active encounter presentation: instead of
    /// reading lines and listing Ink choices, it shows a situation bubble and <b>composes</b> a hand of
    /// typed cards. The hand is:
    /// <list type="bullet">
    /// <item>one <see cref="EncounterCardType.QuestOffer"/> card per Ink choice (pick → <see cref="DialogueRunner.SelectChoice"/>);</item>
    /// <item>an <see cref="EncounterCardType.Attack"/> card — an Ink choice tagged <c>card: attack</c> when
    /// authored (preserves its in-Ink combat consequence), else a system-added card when the casting is
    /// combat-capable (pick → <see cref="DialogueRunner.TriggerCombat"/>);</item>
    /// <item>a <see cref="EncounterCardType.Leave"/> card, always present (pick → <see cref="DialogueRunner.Leave"/>).</item>
    /// </list>
    /// Interaction: a narration line shows alone (no cards while it types); when it finishes revealing the
    /// presenter auto-advances into its choices — there is no continue button. Picking a quest/talk card
    /// shows the branch's closing reply, which a dismiss tap then closes. The fact/quest/tag machinery is
    /// untouched — only presentation + when <see cref="DialogueRunner.Continue"/> fires. Holds no domain
    /// state beyond the currently-shown hand and the closing-reply guard.
    /// </summary>
    public sealed class EncounterCardHandPresenter : IInitializable, IDisposable
    {
        private const string AttackCardLabel = "Attack";
        private const string LeaveCardLabel = "Leave";
        private const string AttackChoiceTag = "card:attack";

        private readonly DialogueRunner _runner;
        private readonly IEncounterCardHandView _view;
        private readonly IGameLogger _logger;

        private readonly List<HandCard> _hand = new List<HandCard>();

        // Per-encounter chrome guards (the runner + presenter are singletons reused across encounters):
        // the portrait and the default speaker name are pushed once when the box first appears, then reset
        // on dialogue end so the next encounter re-seeds them. A #speaker: tag still overrides the name.
        private bool _portraitShown;
        private bool _speakerSet;

        // Set when a quest/talk card is picked: the next line is the picked branch's closing reply, which is
        // shown but must NOT auto-advance — it waits for the player's dismiss tap to close the box. A
        // pre-choice line (this flag false) auto-advances into its choices the moment it finishes revealing.
        private bool _closingReplyPending;

        public EncounterCardHandPresenter(DialogueRunner runner, IEncounterCardHandView view,
            IGameLogger logger = null)
        {
            _runner = runner;
            _view = view;
            _logger = logger;
        }

        public void Initialize()
        {
            _runner.OnSpeakerChanged += HandleSpeakerChanged;
            _runner.OnLine += HandleLine;
            _runner.OnChoices += HandleChoices;
            _runner.OnCombatTriggered += HandleCombatTriggered;
            _runner.OnDialogueEnded += HandleDialogueEnded;

            _view.OnCardSelected += HandleCardSelected;
            _view.OnRevealCompleted += HandleRevealCompleted;
            _view.OnContinueRequested += HandleContinue;
            _view.SetVisible(false);
        }

        public void Dispose()
        {
            _runner.OnSpeakerChanged -= HandleSpeakerChanged;
            _runner.OnLine -= HandleLine;
            _runner.OnChoices -= HandleChoices;
            _runner.OnCombatTriggered -= HandleCombatTriggered;
            _runner.OnDialogueEnded -= HandleDialogueEnded;

            _view.OnCardSelected -= HandleCardSelected;
            _view.OnRevealCompleted -= HandleRevealCompleted;
            _view.OnContinueRequested -= HandleContinue;
        }

        private void HandleSpeakerChanged(string speaker)
        {
            _speakerSet = true;
            _view.SetSpeaker(speaker);
        }

        private void HandleLine(string text)
        {
            // A narration line shows alone — no cards while it types (the hand is cleared). The choice cards
            // are composed only at the decision point (HandleChoices) once the line has fully revealed, so a
            // system-added attack can never be picked mid-narration and bypass an authored Ink combat branch.
            _view.SetVisible(true);
            EnsureEncounterChrome();
            ClearHand();
            _view.ShowSituation(text);
        }

        /// <summary>
        /// A line finished revealing: a pre-choice line auto-advances into its choices (no continue button);
        /// a closing reply (after a quest/talk pick) holds, waiting for the player's dismiss tap to close.
        /// </summary>
        private void HandleRevealCompleted()
        {
            if (_closingReplyPending)
            {
                return;
            }

            _runner.Continue();
        }

        private void HandleChoices(IReadOnlyList<StoryChoice> choices)
        {
            // A decision point: back to interactive, so no closing reply is pending. The Ink choices join the
            // persistent Leave (and a system Attack when combat-capable) as the hand.
            _closingReplyPending = false;
            _view.SetVisible(true);
            EnsureEncounterChrome();
            ShowHand(choices);
        }

        /// <summary>
        /// Pushes the per-encounter chrome the moment the box first appears: the NPC portrait (R2/R3) and,
        /// as a fallback, the NPC's display name as the speaker label when no <c>#speaker:</c> tag has set
        /// one yet. Both run once per encounter; <see cref="HandleDialogueEnded"/> resets the guards.
        /// </summary>
        private void EnsureEncounterChrome()
        {
            if (!_portraitShown)
            {
                _view.SetPortrait(_runner.EncounterArchetypeId);
                _portraitShown = true;
            }

            if (!_speakerSet)
            {
                _view.SetSpeaker(_runner.EncounterDisplayName);
                _speakerSet = true;
            }
        }

        private void HandleCombatTriggered(string enemyId)
        {
            // Combat runs outside Ink; hide the hand until the encounter reports a result (DialogueActiveState
            // routes the combat, then ReportCombatResult resumes the runner and any post-combat line re-shows).
            _view.SetVisible(false);
        }

        private void HandleDialogueEnded(string outcome)
        {
            _hand.Clear();
            _portraitShown = false;
            _speakerSet = false;
            _closingReplyPending = false;
            _view.SetVisible(false);
        }

        // The dismiss tap on a fully-shown closing reply (or a stray tap on a revealed line); ends the
        // encounter via the runner's continue. A no-op when the runner is not parked on a line.
        private void HandleContinue() => _runner.Continue();

        /// <summary>Empties the shown hand and the view's cards — no cards are shown while a line types.</summary>
        private void ClearHand()
        {
            _hand.Clear();
            _view.ShowCards(System.Array.Empty<EncounterCardViewData>());
        }

        private void HandleCardSelected(int index)
        {
            if (index < 0 || index >= _hand.Count)
            {
                _logger?.Warning($"[EncounterCardHandPresenter] Card index {index} is out of range.");
                return;
            }

            var card = _hand[index];
            switch (card.Type)
            {
                case EncounterCardType.QuestOffer:
                case EncounterCardType.Talk:
                    // The picked branch's trailing line is a closing reply: show it, then a tap closes the box.
                    _closingReplyPending = true;
                    _runner.SelectChoice(card.InkIndex);
                    break;
                case EncounterCardType.Attack:
                    if (card.InkIndex >= 0)
                    {
                        _runner.SelectChoice(card.InkIndex); // Ink-authored attack: keep its combat consequence
                    }
                    else
                    {
                        _runner.TriggerCombat(); // system Monster verb
                    }

                    break;
                case EncounterCardType.Leave:
                    _runner.Leave();
                    break;
            }
        }

        /// <summary>
        /// Composes and shows the decision-point hand: each Ink choice first (an Ink <c>card: attack</c>
        /// choice becomes the Attack card), then a system Attack card when the casting is combat-capable and
        /// no Ink choice authored one, then the always-present Leave card.
        ///
        /// An authored combat choice MUST be tagged <c>card: attack</c>; otherwise the presenter cannot
        /// distinguish it and would add a redundant system Attack card beside it.
        /// </summary>
        private void ShowHand(IReadOnlyList<StoryChoice> choices)
        {
            _hand.Clear();
            var cards = new List<EncounterCardViewData>();
            bool inkAttackPresent = false;

            if (choices != null)
            {
                for (int i = 0; i < choices.Count; i++)
                {
                    var choice = choices[i];
                    var type = IsAttackChoice(choice) ? EncounterCardType.Attack : EncounterCardType.QuestOffer;
                    inkAttackPresent |= type == EncounterCardType.Attack;
                    AddCard(cards, type, choice.Index, choice.Text);
                }
            }

            if (_runner.CombatAvailable && !inkAttackPresent)
            {
                AddCard(cards, EncounterCardType.Attack, -1, AttackCardLabel);
            }

            AddCard(cards, EncounterCardType.Leave, -1, LeaveCardLabel);

            _view.ShowCards(cards);
        }

        private void AddCard(List<EncounterCardViewData> cards, EncounterCardType type, int inkIndex,
            string label)
        {
            // A quest card is labelled with the job, not just the reply text (R9): pull the offered quest's
            // title + summary from the casting. The offer-quest tag mints the live instance only once the
            // card is picked, so we read OfferedQuest (the filled slot), not ActiveQuest.
            string questTitle = null;
            string questObjective = null;
            if (type == EncounterCardType.QuestOffer)
            {
                var quest = _runner.OfferedQuest;
                if (quest != null)
                {
                    questTitle = quest.DisplayName;
                    questObjective = quest.Summary;
                }
            }

            cards.Add(new EncounterCardViewData(_hand.Count, label, type, questTitle, questObjective));
            _hand.Add(new HandCard(type, inkIndex));
        }

        private static bool IsAttackChoice(StoryChoice choice)
        {
            if (choice?.Tags == null)
            {
                return false;
            }

            for (int i = 0; i < choice.Tags.Count; i++)
            {
                var tag = choice.Tags[i];
                if (string.IsNullOrEmpty(tag))
                {
                    continue;
                }

                if (tag.Replace(" ", string.Empty).Equals(AttackChoiceTag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>One entry in the shown hand: its type and the Ink choice it maps to (-1 = system card).</summary>
        private readonly struct HandCard
        {
            public EncounterCardType Type { get; }
            public int InkIndex { get; }

            public HandCard(EncounterCardType type, int inkIndex)
            {
                Type = type;
                InkIndex = inkIndex;
            }
        }
    }
}
