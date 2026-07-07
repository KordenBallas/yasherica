using System.Collections.Generic;
using Combat.Data.Providers;
using Combat.Player;
using TMPro;
using UnityEngine;

namespace Combat.View
{
    /// <summary>
    /// World-space row of status glyphs on every combat unit (combat-status-effects S2/FR7):
    /// one icon per active status plus its remaining turns (hidden for permanent part
    /// passives, "×N" suffix for stacks). Sits BELOW the plan-icon row so the two reads
    /// never collide. One manager instance per combat, rows code-built and billboarded —
    /// same idiom as UnitOverheadIconsView. Thin view; the presenter decides content.
    /// </summary>
    public sealed class UnitStatusIconsView : MonoBehaviour, IUnitStatusIconsView
    {
        private ICombatUnitViewRegistry _registry;
        private IStatusEffectDefinitionCatalog _catalog;
        private Camera _camera;

        private readonly Dictionary<int, Transform> _rows = new Dictionary<int, Transform>();

        public void Initialize(ICombatUnitViewRegistry registry, IStatusEffectDefinitionCatalog catalog)
        {
            _registry = registry;
            _catalog = catalog;
            _camera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null) return;
            }

            foreach (var row in _rows.Values)
            {
                if (row != null)
                    row.rotation = _camera.transform.rotation;
            }
        }

        public void SetIcons(int unitId, IReadOnlyList<StatusIconModel> icons)
        {
            if (_registry == null || !_registry.TryGet(unitId, out var visualRoot))
            {
                RemoveRow(unitId);
                return;
            }

            var row = GetOrCreateRow(unitId, visualRoot);

            for (int i = row.childCount - 1; i >= 0; i--)
                Destroy(row.GetChild(i).gameObject);

            if (icons == null || icons.Count == 0)
                return;

            for (int i = 0; i < icons.Count; i++)
            {
                float offsetX = (i - (icons.Count - 1) * 0.5f) * TelegraphStyle.StatusIconSpacing;
                CreateIcon(row, icons[i], new Vector3(offsetX, 0f, 0f));
            }
        }

        public void ClearAll()
        {
            foreach (var row in _rows.Values)
            {
                if (row != null)
                    Destroy(row.gameObject);
            }
            _rows.Clear();
        }

        private Transform GetOrCreateRow(int unitId, Transform visualRoot)
        {
            if (_rows.TryGetValue(unitId, out var existing) && existing != null)
                return existing;

            var rowGo = new GameObject(TelegraphStyle.StatusIconsRowName);
            rowGo.transform.SetParent(visualRoot, false);
            rowGo.transform.localPosition = new Vector3(0f, TelegraphStyle.StatusIconsHeightAboveUnit, 0f);
            _rows[unitId] = rowGo.transform;
            return rowGo.transform;
        }

        private void RemoveRow(int unitId)
        {
            if (_rows.TryGetValue(unitId, out var row) && row != null)
                Destroy(row.gameObject);
            _rows.Remove(unitId);
        }

        private void CreateIcon(Transform row, StatusIconModel model, Vector3 localOffset)
        {
            var iconGo = new GameObject($"Status_{model.StatusId}");
            iconGo.transform.SetParent(row, false);
            iconGo.transform.localPosition = localOffset;

            var spriteGo = new GameObject("Glyph");
            spriteGo.transform.SetParent(iconGo.transform, false);
            var spriteRenderer = spriteGo.AddComponent<SpriteRenderer>();
            if (_catalog != null && _catalog.TryGet(model.StatusId, out var definition) && definition.Glyph != null)
            {
                spriteRenderer.sprite = definition.Glyph;
                var spriteSize = definition.Glyph.bounds.size;
                float largestSide = Mathf.Max(spriteSize.x, spriteSize.y);
                if (largestSide > 0f)
                    spriteGo.transform.localScale = Vector3.one * (TelegraphStyle.StatusIconWorldSize / largestSide);
            }

            // Remaining-turns label: hidden for permanent effects (part passives), "×N" for stacks.
            string labelText = BuildLabel(model);
            if (labelText.Length == 0)
                return;

            var labelGo = new GameObject("Turns");
            labelGo.transform.SetParent(iconGo.transform, false);
            labelGo.transform.localPosition = new Vector3(
                TelegraphStyle.StatusTurnsOffsetX, TelegraphStyle.StatusTurnsOffsetY, 0f);

            var label = labelGo.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = TelegraphStyle.StatusTurnsFontSize;
            label.enableWordWrapping = false;
            label.rectTransform.sizeDelta = new Vector2(1f, 0.5f);
            label.text = labelText;
            label.color = Color.white;
        }

        private static string BuildLabel(StatusIconModel model)
        {
            string turns = model.RemainingTurns >= 0 ? model.RemainingTurns.ToString() : string.Empty;
            string stacks = model.StackCount > 1 ? $"×{model.StackCount}" : string.Empty;
            return stacks.Length > 0 ? $"{turns}{stacks}" : turns;
        }
    }
}
