using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Combat.View
{
    /// <summary>
    /// MonoBehaviour implementation of ICombatActionPanelView.
    /// Thin adapter - contains only Unity UI references and event forwarding.
    /// All business logic lives in CombatActionPanelPresenter.
    /// </summary>
    public class CombatActionPanelView : MonoBehaviour, ICombatActionPanelView
    {
        [Header("Panel Root")]
        [SerializeField] private GameObject _panelRoot;

        [Header("Action Buttons")]
        [SerializeField] private Button _moveButton;
        [SerializeField] private TextMeshProUGUI _moveKeybindText;
        [SerializeField] private Button _changeDirectionButton;
        [SerializeField] private TextMeshProUGUI _changeDirectionKeybindText;
        [SerializeField] private Button _executeQueueButton;
        [SerializeField] private TextMeshProUGUI _executeQueueKeybindText;
        [SerializeField] private TextMeshProUGUI _queueCountText;

        [Header("Ability Slots")]
        [SerializeField] private List<AbilitySlot> _abilitySlots = new List<AbilitySlot>();

        [Header("Highlighting")]
        [SerializeField] private Color _highlightColor = new Color(1f, 1f, 0f, 0.5f);
        [SerializeField] private Color _normalColor = Color.white;

        // Events
        public event Action OnMoveClicked;
        public event Action OnChangeDirectionClicked;
        public event Action OnExecuteQueueClicked;
        public event Action<int> OnAbilityClicked;

        private void Awake()
        {
            // Start hidden - presenter will show when combat begins
            HidePanel();

            // Hook up button click events
            if (_moveButton != null)
                _moveButton.onClick.AddListener(() => OnMoveClicked?.Invoke());

            if (_changeDirectionButton != null)
                _changeDirectionButton.onClick.AddListener(() => OnChangeDirectionClicked?.Invoke());

            if (_executeQueueButton != null)
                _executeQueueButton.onClick.AddListener(() => OnExecuteQueueClicked?.Invoke());

            // Hook up ability slot clicks
            for (int i = 0; i < _abilitySlots.Count; i++)
            {
                int index = i; // Capture for closure
                if (_abilitySlots[i].Button != null)
                {
                    _abilitySlots[i].Button.onClick.AddListener(() => OnAbilityClicked?.Invoke(index));
                }
            }
        }

        public void ShowPanel()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(true);
        }

        public void HidePanel()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }

        public void SetMoveAction(bool available, string keybindLabel)
        {
            if (_moveButton != null)
                _moveButton.interactable = available;

            if (_moveKeybindText != null)
                _moveKeybindText.text = keybindLabel;
        }

        public void SetChangeDirectionAction(bool available, string keybindLabel)
        {
            if (_changeDirectionButton != null)
                _changeDirectionButton.interactable = available;

            if (_changeDirectionKeybindText != null)
                _changeDirectionKeybindText.text = keybindLabel;
        }

        public void SetExecuteQueueAction(bool available, int queueCount, string keybindLabel)
        {
            if (_executeQueueButton != null)
                _executeQueueButton.interactable = available;

            if (_executeQueueKeybindText != null)
                _executeQueueKeybindText.text = keybindLabel;

            if (_queueCountText != null)
                _queueCountText.text = queueCount > 0 ? $"({queueCount})" : "";
        }

        public void SetAbilities(IReadOnlyList<AbilitySlotData> abilities)
        {
            for (int i = 0; i < _abilitySlots.Count; i++)
            {
                if (i < abilities.Count)
                {
                    var data = abilities[i];
                    _abilitySlots[i].SetData(data);
                    _abilitySlots[i].SetActive(true);
                }
                else
                {
                    _abilitySlots[i].SetActive(false);
                }
            }
        }

        public void UpdateAbilityCooldown(int index, int cooldown, bool available)
        {
            if (index >= 0 && index < _abilitySlots.Count)
            {
                _abilitySlots[index].SetCooldown(cooldown, available);
            }
        }

        public void HighlightSelectedAction(int index)
        {
            ClearHighlight();

            if (index >= 0 && index < _abilitySlots.Count)
            {
                _abilitySlots[index].SetHighlight(true, _highlightColor);
            }
        }

        public void ClearHighlight()
        {
            foreach (var slot in _abilitySlots)
            {
                slot.SetHighlight(false, _normalColor);
            }
        }

        /// <summary>
        /// Serializable ability slot component references.
        /// </summary>
        [Serializable]
        public class AbilitySlot
        {
            public Button Button;
            public Image IconImage;
            public TextMeshProUGUI NameText;
            public TextMeshProUGUI KeybindText;
            public TextMeshProUGUI CooldownText;
            public Image CooldownOverlay;
            public Image HighlightImage;

            public void SetData(AbilitySlotData data)
            {
                if (IconImage != null)
                    IconImage.sprite = data.Icon;

                if (NameText != null)
                    NameText.text = data.Name;

                if (KeybindText != null)
                    KeybindText.text = data.KeybindLabel;

                SetCooldown(data.CooldownRemaining, data.IsAvailable);
            }

            public void SetCooldown(int cooldown, bool available)
            {
                if (Button != null)
                    Button.interactable = available;

                if (CooldownText != null)
                {
                    CooldownText.gameObject.SetActive(cooldown > 0);
                    CooldownText.text = cooldown > 0 ? cooldown.ToString() : "";
                }

                if (CooldownOverlay != null)
                    CooldownOverlay.gameObject.SetActive(cooldown > 0);
            }

            public void SetActive(bool active)
            {
                if (Button != null)
                    Button.gameObject.SetActive(active);
            }

            public void SetHighlight(bool highlighted, Color color)
            {
                if (HighlightImage != null)
                {
                    HighlightImage.gameObject.SetActive(highlighted);
                    HighlightImage.color = color;
                }
            }
        }
    }
}
