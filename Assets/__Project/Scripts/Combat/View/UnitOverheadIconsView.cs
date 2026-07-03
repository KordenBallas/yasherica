using System.Collections.Generic;
using Combat.Data.Providers;
using Combat.Player;
using TMPro;
using UnityEngine;

namespace Combat.View
{
    /// <summary>
    /// World-space rows of plan icons above every combat unit (the ghost telegraph's
    /// "plan on the board"). One manager instance per combat: each unit gets a code-built
    /// row parented to its visual root, billboarded to the camera each frame. Icons carry
    /// a collider + AbilityIconMarker so the hover controller can replay their ghost.
    /// Thin view — the presenter decides what each row shows.
    /// </summary>
    public sealed class UnitOverheadIconsView : MonoBehaviour, IUnitPlanIconsView
    {
        private const string MoveGlyph = "»";

        private ICombatUnitViewRegistry _registry;
        private IAbilityDefinitionCatalog _catalog;
        private Camera _camera;

        private readonly Dictionary<int, Transform> _rows = new Dictionary<int, Transform>();

        public void Initialize(ICombatUnitViewRegistry registry, IAbilityDefinitionCatalog catalog)
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

        public void SetIcons(int unitId, IReadOnlyList<PlanIconModel> icons)
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
                float offsetX = (i - (icons.Count - 1) * 0.5f) * TelegraphStyle.IconSpacing;
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

            var rowGo = new GameObject(TelegraphStyle.PlanIconsRowName);
            rowGo.transform.SetParent(visualRoot, false);
            rowGo.transform.localPosition = new Vector3(0f, TelegraphStyle.IconsHeightAboveUnit, 0f);
            _rows[unitId] = rowGo.transform;
            return rowGo.transform;
        }

        private void RemoveRow(int unitId)
        {
            if (_rows.TryGetValue(unitId, out var row) && row != null)
                Destroy(row.gameObject);
            _rows.Remove(unitId);
        }

        private void CreateIcon(Transform row, PlanIconModel model, Vector3 localOffset)
        {
            var iconGo = new GameObject(model.IsMoveIntent ? "MoveIntent" : $"Ability_{model.AbilityId}");
            iconGo.transform.SetParent(row, false);
            iconGo.transform.localPosition = localOffset;

            if (model.IsMoveIntent)
            {
                var glyph = iconGo.AddComponent<TextMeshPro>();
                glyph.alignment = TextAlignmentOptions.Center;
                glyph.fontSize = TelegraphStyle.MoveGlyphFontSize;
                glyph.color = Color.white;
                glyph.enableWordWrapping = false;
                glyph.rectTransform.sizeDelta = new Vector2(1f, 1f);
                glyph.text = MoveGlyph;
            }
            else
            {
                // Sprite lives on a scaled child so the hover collider on the icon root
                // keeps its authored world size.
                var spriteGo = new GameObject("Sprite");
                spriteGo.transform.SetParent(iconGo.transform, false);
                var spriteRenderer = spriteGo.AddComponent<SpriteRenderer>();
                if (_catalog != null && _catalog.TryGet(model.AbilityId, out var definition) && definition.Icon != null)
                {
                    spriteRenderer.sprite = definition.Icon;
                    var spriteSize = definition.Icon.bounds.size;
                    float largestSide = Mathf.Max(spriteSize.x, spriteSize.y);
                    if (largestSide > 0f)
                        spriteGo.transform.localScale = Vector3.one * (TelegraphStyle.IconWorldSize / largestSide);
                }
            }

            var collider = iconGo.AddComponent<BoxCollider>();
            collider.size = new Vector3(TelegraphStyle.IconWorldSize, TelegraphStyle.IconWorldSize, 0.1f);
            collider.isTrigger = true;

            iconGo.AddComponent<AbilityIconMarker>()
                .Initialize(model.UnitId, model.QueueIndex, model.IsEnemyIntent);
        }
    }
}
