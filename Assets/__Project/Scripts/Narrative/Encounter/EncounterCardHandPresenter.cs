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
    /// The fact/quest/tag machinery is untouched — only presentation + choice selection change. Holds no
    /// domain state beyond the currently-shown hand.
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
            _view.OnContinueRequested -= HandleContinue;
        }

        private void HandleSpeakerChanged(string speaker)
        {
            _speakerSet = true;
            _view.SetSpeaker(speaker);
        }

        private void HandleLine(string text)
        {
            // A narration line parks the runner in AwaitingContinue; show the bubble + tap-to-continue with
            // only the persistent Leave card. The Attack card is composed at the decision point (below) so a
            // system-added attack can never be picked mid-narration and bypass an authored Ink combat branch.
            _view.SetVisible(true);
            EnsureEncounterChrome();
            _view.ShowSituation(text);
            _view.ShowContinueAffordance(true);
            ShowHand(null, atDecisionPoint: false);
        }

        private void HandleChoices(IReadOnlyList<StoryChoice> choices)
        {
            // A decision point: the Ink choices join the persistent cards as the hand; no narration gate.
            _view.SetVisible(true);
            EnsureEncounterChrome();
            _view.ShowContinueAffordance(false);
            ShowHand(choices, atDecisionPoint: true);
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
            _view.SetVisible(false);
        }

        private void HandleContinue() => _runner.Continue();

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
        /// Composes and shows the hand: Ink choices first (an Ink <c>card: attack</c> choice becomes the
        /// Attack card), then — only at a decision point — a system Attack card when the casting is
        /// combat-capable and no Ink choice authored one, then the always-present Leave card.
        ///
        /// An authored combat choice MUST be tagged <c>card: attack</c>; otherwise the presenter cannot
        /// distinguish it and would add a redundant system Attack card beside it. The fully system-driven
        /// attack on a choice-less hostile NPC (no decision point) is the deferred Monster verb.
        /// </summary>
        private void ShowHand(IReadOnlyList<StoryChoice> choices, bool atDecisionPoint)
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

            if (atDecisionPoint && _runner.CombatAvailable && !inkAttackPresent)
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
