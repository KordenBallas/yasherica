using System;
using System.Collections;
using System.Collections.Generic;
using Narrative.Actors.Data;
using Narrative.Encounter;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Zenject;

namespace Narrative.View
{
    /// <summary>
    /// Thin adapter for the encounter dialogue UI (the Hades-style bottom box): a portrait + speaker name,
    /// the current line revealed <b>word by word</b> at a tunable reading speed (R4) with a tap that snaps
    /// it to full (R5), a tap-to-continue affordance shown only once the line has finished (R6), and a
    /// centred hand of <see cref="EncounterCardView"/> instances above the box. Author-marked key words
    /// (<c>[[ ]]</c>) are tinted via <see cref="KeywordHighlightFormatter"/> (R11/R12). Holds no domain
    /// logic — the reveal/tap interaction is pure presentation; picks and the continue tap forward to the
    /// presenter.
    /// </summary>
    public class EncounterCardHandView : MonoBehaviour, IEncounterCardHandView
    {
        [Tooltip("Root toggled with the encounter's visibility")]
        [SerializeField] private GameObject _root;

        [Header("Speaker")]
        [SerializeField] private Image _portrait;
        [Tooltip("Shown when the NPC has no portrait so the box still works (R3)")]
        [SerializeField] private Sprite _placeholderPortrait;
        [SerializeField] private TextMeshProUGUI _speakerLabel;

        [Header("Line")]
        [SerializeField] private TextMeshProUGUI _situationText;
        [Tooltip("Reading speed for the word-by-word reveal — the single global setting (R4)")]
        [SerializeField] private float _wordsPerSecond = 8f;
        [Tooltip("Tint applied to author-marked [[key words]] (R11)")]
        [SerializeField] private Color _keywordColor = new Color(1f, 0.82f, 0.4f);
        [Tooltip("Full-box tap target: tap to complete the reveal, then to continue (R5)")]
        [SerializeField] private Button _tapArea;
        [Tooltip("Tap-to-continue affordance shown once the line has finished revealing")]
        [SerializeField] private GameObject _continueAffordance;
        [SerializeField] private Button _continueButton;

        [Header("Cards")]
        [Tooltip("Parent transform the card hand is laid out under (centred above the box)")]
        [SerializeField] private Transform _cardAnchor;
        [SerializeField] private EncounterCardView _cardPrefab;

        private readonly List<EncounterCardView> _cards = new List<EncounterCardView>();

        [Inject] private INpcArchetypeCatalog _archetypeCatalog;

        private Coroutine _revealRoutine;
        private bool _revealing;
        private bool _continuePending; // the presenter declared the current line continuable
        private string _keywordHex;

        public event Action<int> OnCardSelected;
        public event Action OnContinueRequested;

        private void Awake()
        {
            EnsureEventSystemExists();
            _keywordHex = ColorUtility.ToHtmlStringRGB(_keywordColor);

            if (_continueButton != null)
            {
                _continueButton.onClick.AddListener(HandleTap);
            }

            if (_tapArea != null)
            {
                _tapArea.onClick.AddListener(HandleTap);
            }

            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        // The card hand owns the encounter UI now that the dialogue panel is retired; make sure an
        // EventSystem exists so the cards/continue button receive input even if the scene lacks one.
        private static void EnsureEventSystemExists()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private void OnDestroy()
        {
            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveListener(HandleTap);
            }

            if (_tapArea != null)
            {
                _tapArea.onClick.RemoveListener(HandleTap);
            }
        }

        public void SetSpeaker(string name)
        {
            if (_speakerLabel != null)
            {
                _speakerLabel.text = name ?? string.Empty;
            }
        }

        public void SetPortrait(string archetypeId)
        {
            if (_portrait == null)
            {
                return;
            }

            Sprite sprite = null;
            if (!string.IsNullOrEmpty(archetypeId) && _archetypeCatalog != null)
            {
                var archetype = _archetypeCatalog.Get(archetypeId);
                sprite = archetype != null ? archetype.Portrait : null;
            }

            _portrait.sprite = sprite != null ? sprite : _placeholderPortrait;
            _portrait.enabled = _portrait.sprite != null;
        }

        public void ShowSituation(string line)
        {
            if (_situationText == null)
            {
                return;
            }

            ShowGlyph(false); // hidden until the reveal finishes (R6)
            _situationText.text = KeywordHighlightFormatter.ToRichText(line ?? string.Empty, _keywordHex);
            _situationText.maxVisibleCharacters = 0;
            StartReveal();
        }

        public void ShowContinueAffordance(bool visible)
        {
            _continuePending = visible;
            // Defer showing the glyph until the word-by-word reveal completes; hide immediately when the
            // line is no longer continuable (a decision point passes false).
            ShowGlyph(visible && !_revealing);
        }

        public void ShowCards(IReadOnlyList<EncounterCardViewData> cards)
        {
            ClearCards();
            if (cards == null || _cardPrefab == null || _cardAnchor == null)
            {
                return;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                var card = Instantiate(_cardPrefab, _cardAnchor);
                card.Configure(i, cards[i], _keywordHex, HandleCardSelected);
                _cards.Add(card);
            }
        }

        public void SetVisible(bool visible)
        {
            if (_root != null)
            {
                _root.SetActive(visible);
            }

            if (!visible)
            {
                StopReveal();
                ClearCards();
            }
        }

        private void StartReveal()
        {
            StopReveal();

            // No text to reveal, or a non-positive speed → show the whole line at once.
            _situationText.ForceMeshUpdate();
            int total = _situationText.textInfo.characterCount;
            if (total <= 0 || _wordsPerSecond <= 0f)
            {
                CompleteReveal();
                return;
            }

            _revealing = true;
            _revealRoutine = StartCoroutine(RevealWords(BuildWordBoundaries(total)));
        }

        // Visible-character counts at the end of each word (TMP's characterCount excludes rich-text tags,
        // so [[key word]] colour spans never throw off the boundaries — whole words reveal, colour intact).
        private List<int> BuildWordBoundaries(int total)
        {
            var boundaries = new List<int>();
            var info = _situationText.textInfo;
            bool inWord = false;
            for (int i = 0; i < total; i++)
            {
                bool whitespace = char.IsWhiteSpace(info.characterInfo[i].character);
                if (!whitespace)
                {
                    inWord = true;
                }

                bool atWordEnd = inWord &&
                    (i == total - 1 || char.IsWhiteSpace(info.characterInfo[i + 1].character));
                if (atWordEnd)
                {
                    boundaries.Add(i + 1);
                    inWord = false;
                }
            }

            return boundaries;
        }

        private IEnumerator RevealWords(List<int> boundaries)
        {
            var wait = new WaitForSeconds(1f / _wordsPerSecond);
            for (int i = 0; i < boundaries.Count; i++)
            {
                _situationText.maxVisibleCharacters = boundaries[i];
                yield return wait;
            }

            CompleteReveal();
        }

        // Snaps the line to fully visible and surfaces the continue glyph if the line is gated.
        private void CompleteReveal()
        {
            if (_revealRoutine != null)
            {
                StopCoroutine(_revealRoutine);
                _revealRoutine = null;
            }

            _revealing = false;
            if (_situationText != null)
            {
                _situationText.maxVisibleCharacters = int.MaxValue;
            }

            if (_continuePending)
            {
                ShowGlyph(true);
            }
        }

        private void StopReveal()
        {
            if (_revealRoutine != null)
            {
                StopCoroutine(_revealRoutine);
                _revealRoutine = null;
            }

            _revealing = false;
        }

        // A tap mid-reveal completes the line (R5); a tap once revealed advances the gated line (R6).
        private void HandleTap()
        {
            if (_revealing)
            {
                CompleteReveal();
                return;
            }

            OnContinueRequested?.Invoke();
        }

        private void ShowGlyph(bool visible)
        {
            if (_continueAffordance != null)
            {
                _continueAffordance.SetActive(visible);
            }
        }

        private void HandleCardSelected(int index) => OnCardSelected?.Invoke(index);

        private void ClearCards()
        {
            foreach (var card in _cards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            _cards.Clear();
        }
    }
}
