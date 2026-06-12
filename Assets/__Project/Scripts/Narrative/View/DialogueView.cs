using System;
using System.Collections;
using System.Collections.Generic;
using Narrative.Dialogue;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using TMPro;

namespace Narrative.View
{
    /// <summary>
    /// MonoBehaviour adapter for dialogue UI.
    /// Contains NO business logic - only UI management.
    /// </summary>
    public class DialogueView : MonoBehaviour, IDialogueView
    {
        [Header("Panel References")]
        [SerializeField] private GameObject _dialoguePanel;
        [SerializeField] private GameObject _choicesPanel;
        [Tooltip("Fullscreen modal dim behind the dialogue; must hide with the panel or it keeps swallowing every pointer event in the scene")]
        [SerializeField] private GameObject _backgroundDim;

        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI _speakerNameText;
        [SerializeField] private TextMeshProUGUI _dialogueText;

        [Header("Portrait")]
        [SerializeField] private Image _portraitImage;
        [SerializeField] private GameObject _portraitContainer;

        [Header("Buttons")]
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _skipButton;

        [Header("Choice Configuration")]
        [SerializeField] private Transform _choicesContainer;
        [SerializeField] private Button _choiceButtonPrefab;

        [Header("Typewriter Settings")]
        [SerializeField] private float _defaultCharsPerSecond = 30f;

        private readonly List<Button> _choiceButtons = new();
        private Coroutine _typewriterCoroutine;
        private string _fullText;
        private Action _onTypewriterComplete;

        public event Action OnContinueClicked;
        public event Action<int> OnChoiceSelected;
        public event Action OnSkipRequested;

        private void Awake()
        {
            ValidateUISetup();
            SetupButtons();
            Hide();
        }

        private void ValidateUISetup()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[DialogueView] No Canvas found in parent hierarchy!");
                return;
            }

            var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster == null)
            {
                Debug.LogError("[DialogueView] Canvas missing GraphicRaycaster component!");
            }
            else
            {
                Debug.Log($"[DialogueView] GraphicRaycaster found and enabled: {raycaster.enabled}");
            }

            EnsureEventSystemExists();
        }

        private void EnsureEventSystemExists()
        {
            var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                Debug.LogWarning("[DialogueView] No EventSystem found - creating one automatically");
                var eventSystemObj = new GameObject("EventSystem");
                eventSystem = eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<InputSystemUIInputModule>();
                Debug.Log("[DialogueView] EventSystem created successfully with InputSystemUIInputModule");
            }
            else if (!eventSystem.enabled)
            {
                Debug.LogError("[DialogueView] EventSystem is disabled!");
            }
            else
            {
                Debug.Log("[DialogueView] EventSystem found and enabled");
            }
        }

        private void SetupButtons()
        {
            if (_continueButton != null)
            {
                _continueButton.onClick.AddListener(() => OnContinueClicked?.Invoke());
            }

            if (_skipButton != null)
            {
                _skipButton.onClick.AddListener(() => OnSkipRequested?.Invoke());
            }
        }

        public void Show()
        {
            if (_backgroundDim != null)
            {
                _backgroundDim.SetActive(true);
            }

            if (_dialoguePanel != null)
            {
                _dialoguePanel.SetActive(true);
            }
        }

        public void Hide()
        {
            StopTypewriter();

            if (_dialoguePanel != null)
            {
                _dialoguePanel.SetActive(false);
            }

            // The dim is a raycast target covering the whole screen; leaving it
            // active outside dialogues blocks clicks for every system below this canvas.
            if (_backgroundDim != null)
            {
                _backgroundDim.SetActive(false);
            }

            HideChoices();
        }

        public void SetSpeakerName(string name)
        {
            if (_speakerNameText != null)
            {
                _speakerNameText.text = name ?? string.Empty;
            }
        }

        public void SetDialogueText(string text)
        {
            StopTypewriter();

            if (_dialogueText != null)
            {
                _dialogueText.text = text ?? string.Empty;
            }
        }

        public void SetPortrait(Sprite portrait)
        {
            if (_portraitImage != null)
            {
                _portraitImage.sprite = portrait;
                _portraitImage.enabled = portrait != null;
            }

            if (_portraitContainer != null)
            {
                _portraitContainer.SetActive(portrait != null);
            }
        }

        public void ClearPortrait()
        {
            if (_portraitImage != null)
            {
                _portraitImage.sprite = null;
                _portraitImage.enabled = false;
            }

            if (_portraitContainer != null)
            {
                _portraitContainer.SetActive(false);
            }
        }

        public void ShowChoices(IReadOnlyList<DialogueChoice> choices)
        {
            ClearChoiceButtons();

            Debug.Log($"[DialogueView] ShowChoices called with {choices?.Count ?? 0} choices");

            if (_choicesPanel == null || _choicesContainer == null || _choiceButtonPrefab == null)
            {
                Debug.LogError("[DialogueView] Choice UI elements not configured!");
                Debug.LogError($"  _choicesPanel null? {_choicesPanel == null}");
                Debug.LogError($"  _choicesContainer null? {_choicesContainer == null}");
                Debug.LogError($"  _choiceButtonPrefab null? {_choiceButtonPrefab == null}");
                return;
            }

            _choicesPanel.SetActive(true);

            foreach (var choice in choices)
            {
                var button = Instantiate(_choiceButtonPrefab, _choicesContainer);
                button.gameObject.SetActive(true);

                Debug.Log($"[DialogueView] Created choice button {choice.Index}: '{choice.Text}', IsEnabled={choice.IsEnabled}");

                var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = choice.Text;
                }
                else
                {
                    Debug.LogError($"[DialogueView] Choice button {choice.Index} has no TextMeshProUGUI!");
                }

                button.interactable = choice.IsEnabled;
                Debug.Log($"[DialogueView] Button {choice.Index} interactable set to: {button.interactable}");

                int choiceIndex = choice.Index;
                button.onClick.AddListener(() => {
                    Debug.Log($"[DialogueView] Button {choiceIndex} CLICKED!");
                    OnChoiceSelected?.Invoke(choiceIndex);
                });

                _choiceButtons.Add(button);
            }

            Debug.Log($"[DialogueView] Total buttons created: {_choiceButtons.Count}");
        }

        public void HideChoices()
        {
            ClearChoiceButtons();

            if (_choicesPanel != null)
            {
                _choicesPanel.SetActive(false);
            }
        }

        public void ShowContinueButton()
        {
            if (_continueButton != null)
            {
                _continueButton.gameObject.SetActive(true);
            }
        }

        public void HideContinueButton()
        {
            if (_continueButton != null)
            {
                _continueButton.gameObject.SetActive(false);
            }
        }

        public void SetSkipEnabled(bool enabled)
        {
            if (_skipButton != null)
            {
                _skipButton.interactable = enabled;
            }
        }

        public void PlayTypewriterEffect(string text, float charsPerSecond, Action onComplete)
        {
            StopTypewriter();

            _fullText = text;
            _onTypewriterComplete = onComplete;

            float speed = charsPerSecond > 0 ? charsPerSecond : _defaultCharsPerSecond;
            _typewriterCoroutine = StartCoroutine(TypewriterCoroutine(text, speed));
        }

        public void SkipTypewriterEffect()
        {
            StopTypewriter();
            SetDialogueText(_fullText);
            _onTypewriterComplete?.Invoke();
        }

        private IEnumerator TypewriterCoroutine(string text, float charsPerSecond)
        {
            if (_dialogueText == null)
                yield break;

            _dialogueText.text = string.Empty;
            float delay = 1f / charsPerSecond;

            for (int i = 0; i <= text.Length; i++)
            {
                _dialogueText.text = text.Substring(0, i);
                yield return new WaitForSeconds(delay);
            }

            _typewriterCoroutine = null;
            _onTypewriterComplete?.Invoke();
        }

        private void StopTypewriter()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }
        }

        private void ClearChoiceButtons()
        {
            foreach (var button in _choiceButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            _choiceButtons.Clear();
        }

        private void OnDestroy()
        {
            StopTypewriter();
            ClearChoiceButtons();

            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveAllListeners();
            }

            if (_skipButton != null)
            {
                _skipButton.onClick.RemoveAllListeners();
            }
        }
    }
}
