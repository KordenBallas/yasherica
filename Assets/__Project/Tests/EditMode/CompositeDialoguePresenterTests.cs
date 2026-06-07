using System;
using System.Collections.Generic;
using Narrative;
using Narrative.Data.Definitions;
using Narrative.Dialogue;
using Narrative.Generation;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class CompositeDialoguePresenterTests
    {
        private MockStoryManager _storyMgr;
        private MockStoryManager _npcMgr;
        private MockDialogueView _view;
        private CompositeDialoguePresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _storyMgr = new MockStoryManager();
            _npcMgr = new MockStoryManager();
            _view = new MockDialogueView();
            _presenter = new CompositeDialoguePresenter(_storyMgr, _npcMgr, _view);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
        }

        private NpcAssignment CreateAssignment(bool hasStory = true, bool hasCharacterInk = true)
        {
            var npc = ScriptableObject.CreateInstance<NpcDefinition>();
            var so = new UnityEditor.SerializedObject(npc);
            so.FindProperty("_npcId").stringValue = "test_npc";
            so.FindProperty("_displayName").stringValue = "Test NPC";
            so.FindProperty("_characterStartKnot").stringValue = "greeting";
            if (hasCharacterInk)
            {
                var charInk = new TextAsset("{\"inkVersion\":21}");
                so.FindProperty("_characterInkJson").objectReferenceValue = charInk;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            StoryDefinition story = null;
            if (hasStory)
            {
                story = ScriptableObject.CreateInstance<StoryDefinition>();
                var storySo = new UnityEditor.SerializedObject(story);
                storySo.FindProperty("_storyId").stringValue = "test_story";
                storySo.FindProperty("_startingKnot").stringValue = "start";
                var inkAsset = new TextAsset("{\"inkVersion\":21}");
                storySo.FindProperty("_inkJsonAsset").objectReferenceValue = inkAsset;
                storySo.ApplyModifiedPropertiesWithoutUndo();
            }

            return new NpcAssignment(npc, story, null);
        }

        [Test]
        public void StartDialogue_WithAssignment_ShowsView()
        {
            _storyMgr.SetupContinue("Hello there.", canContinueAfter: false);
            _npcMgr.SetupContinue("*grumbles*", canContinueAfter: false);

            var assignment = CreateAssignment();
            _presenter.StartDialogue(assignment);

            Assert.IsTrue(_view.IsShown);
            Assert.IsTrue(_presenter.IsActive);
        }

        [Test]
        public void StartDialogue_AdvancesStoryFirst()
        {
            _storyMgr.SetupContinue("Quest text.", canContinueAfter: false);
            _npcMgr.SetupContinue("NPC greeting.", canContinueAfter: false);

            var assignment = CreateAssignment();
            _presenter.StartDialogue(assignment);

            // Story should be advanced first
            Assert.AreEqual("Quest text.", _view.LastText);
        }

        [Test]
        public void StartDialogue_SetsNpcNameVariable()
        {
            _storyMgr.SetupContinue("Hello.", canContinueAfter: false);

            var assignment = CreateAssignment();
            _presenter.StartDialogue(assignment);

            Assert.AreEqual("npc_name", _storyMgr.LastSetVariableName);
            Assert.AreEqual("Test NPC", _storyMgr.LastSetVariableValue);
        }

        [Test]
        public void SelectChoice_RoutesToCorrectManager_StoryChoice()
        {
            _storyMgr.SetupContinue("Text.", canContinueAfter: false);
            _storyMgr.SetupChoices(new List<StoryChoice>
            {
                new StoryChoice(0, "Story option A"),
                new StoryChoice(1, "Story option B")
            });
            _npcMgr.SetupChoices(new List<StoryChoice>
            {
                new StoryChoice(0, "NPC option")
            });

            var assignment = CreateAssignment();
            _presenter.StartDialogue(assignment);

            // Choice 0 should be story choice 0
            _storyMgr.SetupContinue("After choice.", canContinueAfter: false);
            _storyMgr.ClearChoices();
            _npcMgr.ClearChoices();
            _presenter.SelectChoice(0);

            Assert.AreEqual(0, _storyMgr.LastChosenIndex);
            Assert.AreEqual(-1, _npcMgr.LastChosenIndex);
        }

        [Test]
        public void SelectChoice_RoutesToCorrectManager_NpcChoice()
        {
            _storyMgr.SetupContinue("Text.", canContinueAfter: false);
            _storyMgr.SetupChoices(new List<StoryChoice>
            {
                new StoryChoice(0, "Story option")
            });
            _npcMgr.SetupChoices(new List<StoryChoice>
            {
                new StoryChoice(0, "NPC option A"),
                new StoryChoice(1, "NPC option B")
            });

            var assignment = CreateAssignment();
            _presenter.StartDialogue(assignment);

            // Choice index 1 = story has 1 choice (index 0), so index 1 = NPC choice 0
            _storyMgr.SetupContinue("After.", canContinueAfter: false);
            _storyMgr.ClearChoices();
            _npcMgr.ClearChoices();
            _presenter.SelectChoice(1);

            Assert.AreEqual(-1, _storyMgr.LastChosenIndex);
            Assert.AreEqual(0, _npcMgr.LastChosenIndex);
        }

        [Test]
        public void CombinedChoices_ContainsBothSources()
        {
            _storyMgr.SetupContinue("Text.", canContinueAfter: false);
            _storyMgr.SetupChoices(new List<StoryChoice>
            {
                new StoryChoice(0, "Story A"),
                new StoryChoice(1, "Story B")
            });
            _npcMgr.SetupChoices(new List<StoryChoice>
            {
                new StoryChoice(0, "NPC C")
            });

            var assignment = CreateAssignment();
            _presenter.StartDialogue(assignment);

            Assert.AreEqual(3, _view.LastChoices.Count);
            Assert.AreEqual("Story A", _view.LastChoices[0].Text);
            Assert.AreEqual("Story B", _view.LastChoices[1].Text);
            Assert.AreEqual("NPC C", _view.LastChoices[2].Text);
        }

        [Test]
        public void EndDialogue_FiresEvent()
        {
            _storyMgr.SetupContinue("Text.", canContinueAfter: false);

            DialogueOutcomeType? receivedOutcome = null;
            _presenter.OnDialogueEnded += outcome => receivedOutcome = outcome;

            var assignment = CreateAssignment();
            _presenter.StartDialogue(assignment);
            _presenter.EndDialogue(DialogueOutcomeType.Exit);

            Assert.AreEqual(DialogueOutcomeType.Exit, receivedOutcome);
            Assert.IsFalse(_presenter.IsActive);
            Assert.IsFalse(_view.IsShown);
        }

        [Test]
        public void StartDialogue_CharacterOnly_UsesNpcManagerOnly()
        {
            _npcMgr.SetupContinue("NPC text.", canContinueAfter: false);

            var assignment = CreateAssignment(hasStory: false, hasCharacterInk: true);
            _presenter.StartDialogue(assignment);

            Assert.AreEqual("NPC text.", _view.LastText);
            Assert.IsFalse(_storyMgr.WasLoaded);
        }

        [Test]
        public void ProcessTag_OutcomeCombat_EndsDialogue()
        {
            _storyMgr.SetupContinue("Fight!", canContinueAfter: false,
                tags: new List<string> { "outcome: Combat" });

            DialogueOutcomeType? received = null;
            _presenter.OnDialogueEnded += o => received = o;

            var assignment = CreateAssignment();
            _presenter.StartDialogue(assignment);

            Assert.AreEqual(DialogueOutcomeType.Combat, received);
        }

        // --- Mock implementations ---

        private class MockStoryManager : IStoryManager
        {
            private string _nextText;
            private bool _canContinueAfter;
            private List<StoryChoice> _choices = new();
            private List<string> _tags = new();

            public bool WasLoaded { get; private set; }
            public int LastChosenIndex { get; private set; } = -1;
            public string LastSetVariableName { get; private set; }
            public object LastSetVariableValue { get; private set; }

            public event Action<string> OnTextChanged;
            public event Action<IReadOnlyList<StoryChoice>> OnChoicesAvailable;
            public event Action OnStoryEnded;
            public event Action<IReadOnlyList<string>> OnTagsParsed;

            public bool CanContinue => _nextText != null;
            public bool HasChoices => _choices.Count > 0;
            public IReadOnlyList<StoryChoice> CurrentChoices => _choices;
            public IReadOnlyList<string> CurrentTags => _tags;
            public string CurrentText => _nextText ?? string.Empty;

            public void SetupContinue(string text, bool canContinueAfter = false, List<string> tags = null)
            {
                _nextText = text;
                _canContinueAfter = canContinueAfter;
                _tags = tags ?? new List<string>();
            }

            public void SetupChoices(List<StoryChoice> choices)
            {
                _choices = choices ?? new List<StoryChoice>();
            }

            public void ClearChoices()
            {
                _choices.Clear();
            }

            public void LoadStory(string jsonContent)
            {
                WasLoaded = true;
            }

            public string Continue()
            {
                var text = _nextText ?? string.Empty;
                if (!_canContinueAfter)
                    _nextText = null;
                return text;
            }

            public void ChooseChoice(int choiceIndex) => LastChosenIndex = choiceIndex;
            public void GoToKnot(string knotName) { }
            public object GetVariable(string variableName) => null;

            public void SetVariable(string variableName, object value)
            {
                LastSetVariableName = variableName;
                LastSetVariableValue = value;
            }

            public int GetVisitCount(string pathString) => 0;
            public IReadOnlyList<string> GetTagsForKnot(string knotName) => Array.Empty<string>();
            public void BindExternalFunction<TResult>(string functionName, Func<TResult> function) { }
            public void BindExternalFunction<T1, TResult>(string functionName, Func<T1, TResult> function) { }
            public void BindExternalFunction<T1>(string functionName, Action<T1> function) { }
            public void BindExternalFunction<T1, T2, TResult>(string functionName, Func<T1, T2, TResult> function) { }
            public void BindExternalFunction<T1, T2>(string functionName, Action<T1, T2> function) { }
            public string SaveState() => string.Empty;
            public void LoadState(string savedState) { }
            public void ResetStory() { }
        }

        private class MockDialogueView : IDialogueView
        {
            public bool IsShown { get; private set; }
            public string LastText { get; private set; }
            public string LastSpeaker { get; private set; }
            public IReadOnlyList<DialogueChoice> LastChoices { get; private set; }

            public event Action OnContinueClicked;
            public event Action<int> OnChoiceSelected;
            public event Action OnSkipRequested;

            public void Show() => IsShown = true;
            public void Hide() => IsShown = false;
            public void SetSpeakerName(string name) => LastSpeaker = name;
            public void SetDialogueText(string text) => LastText = text;
            public void SetPortrait(Sprite portrait) { }
            public void ClearPortrait() { }
            public void ShowChoices(IReadOnlyList<DialogueChoice> choices) => LastChoices = choices;
            public void HideChoices() { }
            public void ShowContinueButton() { }
            public void HideContinueButton() { }
            public void SetSkipEnabled(bool enabled) { }
            public void PlayTypewriterEffect(string text, float charsPerSecond, Action onComplete) { }
            public void SkipTypewriterEffect() { }
        }
    }
}
