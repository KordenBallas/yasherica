using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Combat.Arena.View
{
    /// <summary>
    /// Thin adapter for the clicked-part popover: fills the pre-authored ability rows (icon +
    /// label + hover relay), toggles the Draft button and the note line, and clamps itself to
    /// the screen. The rows' hovers and the button are surfaced as plain events — legality
    /// and preview payloads are the presenter's.
    /// </summary>
    public class ArenaPartInfoPopoverView : MonoBehaviour, IArenaPartInfoPopover
    {
        [Serializable]
        private class AbilityRow
        {
            public GameObject Root;
            public Image Icon;
            public TextMeshProUGUI Label;
            public Mutation.View.PointerHoverRelay HoverRelay;
        }

        [SerializeField] private RectTransform _root;
        [SerializeField] private TextMeshProUGUI _partNameLabel;
        [SerializeField] private TextMeshProUGUI _slotLabel;
        [SerializeField] private List<AbilityRow> _abilityRows = new List<AbilityRow>();
        [SerializeField] private TextMeshProUGUI _noteLabel;
        [SerializeField] private Button _draftButton;
        [Tooltip("Offset from the clicked part's screen position")]
        [SerializeField] private Vector2 _screenOffset = new Vector2(180f, 0f);

        public event Action<int, Vector2, bool> AbilityHovered;
        public event Action DraftClicked;

        private void Awake()
        {
            if (_draftButton != null)
            {
                _draftButton.onClick.AddListener(() => DraftClicked?.Invoke());
            }

            for (int i = 0; i < _abilityRows.Count; i++)
            {
                int index = i;
                var row = _abilityRows[i];
                if (row?.HoverRelay == null)
                {
                    continue;
                }

                row.HoverRelay.Entered += () =>
                    AbilityHovered?.Invoke(index, RowScreenPosition(index), true);
                row.HoverRelay.Exited += () =>
                    AbilityHovered?.Invoke(index, RowScreenPosition(index), false);
            }

            if (_root != null)
            {
                _root.gameObject.SetActive(false);
            }
        }

        public void Show(ArenaPartInfoViewData data, Vector2 screenPosition)
        {
            if (data == null)
            {
                return;
            }

            if (_partNameLabel != null)
            {
                _partNameLabel.text = data.PartName;
            }

            if (_slotLabel != null)
            {
                _slotLabel.text = data.SlotLabel;
            }

            for (int i = 0; i < _abilityRows.Count; i++)
            {
                var row = _abilityRows[i];
                if (row?.Root == null)
                {
                    continue;
                }

                bool used = i < data.Abilities.Count;
                row.Root.SetActive(used);
                if (!used)
                {
                    continue;
                }

                if (row.Label != null)
                {
                    row.Label.text = data.Abilities[i].Label;
                }

                if (row.Icon != null)
                {
                    row.Icon.sprite = data.Abilities[i].Icon;
                    row.Icon.enabled = data.Abilities[i].Icon != null;
                }
            }

            if (_noteLabel != null)
            {
                _noteLabel.text = data.Note;
                _noteLabel.gameObject.SetActive(!string.IsNullOrEmpty(data.Note));
            }

            if (_draftButton != null)
            {
                _draftButton.gameObject.SetActive(data.CanDraft);
            }

            Place(screenPosition + _screenOffset);
            if (_root != null)
            {
                _root.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
            }
        }

        private Vector2 RowScreenPosition(int index)
        {
            var row = _abilityRows[index];
            return row?.Root != null ? (Vector2)row.Root.transform.position : Vector2.zero;
        }

        private void Place(Vector2 screenPosition)
        {
            if (_root == null)
            {
                return;
            }

            // Screen-space-overlay: world position == screen position. Clamp by the popover's
            // own scaled extents so it never leaves the screen.
            var extents = _root.rect.size * _root.lossyScale * 0.5f;
            var clamped = new Vector2(
                Mathf.Clamp(screenPosition.x, extents.x, Screen.width - extents.x),
                Mathf.Clamp(screenPosition.y, extents.y, Screen.height - extents.y));
            _root.position = clamped;
        }
    }
}
