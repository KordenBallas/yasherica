using System.Collections.Generic;
using Combat.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Combat.View
{
    /// <summary>
    /// Code-built screen-space turn-order strip (D2): a horizontal row of per-actor cells anchored in
    /// the top-right corner, matching the code-built-overlay convention of the combat readability layer
    /// (no new prefab; the ghost/plan-icon views are built the same way). Thin adapter — the pure
    /// <see cref="TurnOrderStripPresenter"/> decides the order and status; this only renders it.
    ///
    /// Demo placement per the brief: the final HUD layout, portraits, and theming are a later UI pass.
    /// </summary>
    public sealed class TurnOrderStripView : MonoBehaviour, ITurnOrderStripView
    {
        private const float CellMinWidth = 96f;
        private const float CellHeight = 34f;
        private const float CellSpacing = 6f;
        private const float StripMargin = 12f;
        private const float LabelFontSize = 16f;

        private static readonly Color PlayerColor = new Color(0.20f, 0.42f, 0.30f, 0.92f);
        private static readonly Color EnemyColor = new Color(0.45f, 0.20f, 0.22f, 0.92f);
        private static readonly Color CurrentOutline = new Color(1f, 0.85f, 0.30f, 1f);
        private static readonly Color ActedTint = new Color(0.30f, 0.30f, 0.32f, 0.55f);

        private RectTransform _container;
        private readonly List<GameObject> _cells = new List<GameObject>();

        private void Awake()
        {
            BuildCanvas();
        }

        public void SetEntries(IReadOnlyList<TurnOrderEntryModel> entries)
        {
            Clear();
            if (entries == null) return;

            foreach (var entry in entries)
            {
                _cells.Add(BuildCell(entry));
            }
        }

        public void Clear()
        {
            foreach (var cell in _cells)
            {
                if (cell != null) Destroy(cell);
            }

            _cells.Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }

        private void BuildCanvas()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // above the world, alongside the other combat HUD
            gameObject.AddComponent<CanvasScaler>();
            // No GraphicRaycaster: the strip is a pure read-out and must never intercept input.

            var containerGo = new GameObject("TurnOrderStrip", typeof(RectTransform));
            _container = containerGo.GetComponent<RectTransform>();
            _container.SetParent(transform, worldPositionStays: false);
            // Anchor + pivot to the top-right corner (demo placement).
            _container.anchorMin = new Vector2(1f, 1f);
            _container.anchorMax = new Vector2(1f, 1f);
            _container.pivot = new Vector2(1f, 1f);
            _container.anchoredPosition = new Vector2(-StripMargin, -StripMargin);

            var layout = containerGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = CellSpacing;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var fitter = containerGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private GameObject BuildCell(TurnOrderEntryModel entry)
        {
            var cellGo = new GameObject($"Actor_{entry.UnitId}", typeof(RectTransform), typeof(Image));
            cellGo.transform.SetParent(_container, worldPositionStays: false);

            var background = cellGo.GetComponent<Image>();
            background.color = BackgroundFor(entry);
            background.raycastTarget = false; // read-out only

            var layoutElement = cellGo.AddComponent<LayoutElement>();
            layoutElement.minWidth = CellMinWidth;
            layoutElement.minHeight = CellHeight;
            layoutElement.preferredHeight = CellHeight;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(cellGo.transform, worldPositionStays: false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 2f);
            labelRect.offsetMax = new Vector2(-8f, -2f);

            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = entry.IsCurrent ? $"▶ {entry.Name}" : entry.Name;
            label.fontSize = LabelFontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.color = entry.HasActed ? new Color(0.75f, 0.75f, 0.78f, 0.8f) : Color.white;
            label.raycastTarget = false; // read-out only

            return cellGo;
        }

        private static Color BackgroundFor(TurnOrderEntryModel entry)
        {
            if (entry.HasActed) return ActedTint;
            var baseColor = entry.IsPlayer ? PlayerColor : EnemyColor;
            // The current actor reads brightest (blended toward the highlight outline colour).
            return entry.IsCurrent ? Color.Lerp(baseColor, CurrentOutline, 0.35f) : baseColor;
        }
    }
}
