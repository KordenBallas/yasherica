using System.Collections.Generic;
using Combat.Data.Providers;
using Combat.Player;
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
        private ICombatUnitViewRegistry _registry;
        private IAbilityDefinitionCatalog _catalog;
        private Camera _camera;

        private readonly Dictionary<int, Transform> _rows = new Dictionary<int, Transform>();

        // D3 readiness cue: rows built from enemy committed intents read as restless (jitter/pulse) so a
        // wound-up threat reads at a glance. Uniform across all armed enemies; cleared when the row empties.
        private readonly HashSet<int> _armedEnemyRows = new HashSet<int>();

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

            float baseHeight = TelegraphStyle.IconsHeightAboveUnit;
            float phase = Time.time * TelegraphStyle.ReadyCuePulseSpeed;

            foreach (var kv in _rows)
            {
                var row = kv.Value;
                if (row == null)
                    continue;

                row.rotation = _camera.transform.rotation;

                if (_armedEnemyRows.Contains(kv.Key))
                {
                    // Restless: a small vertical jitter + a scale pulse so the queued threat reads as loaded.
                    float jitter = Mathf.Sin(phase) * TelegraphStyle.ReadyIconJitterAmplitude;
                    float pulse = 1f + (Mathf.Sin(phase) * 0.5f + 0.5f) * TelegraphStyle.ReadyIconPulseAmplitude;
                    row.localPosition = new Vector3(0f, baseHeight + jitter, 0f);
                    row.localScale = Vector3.one * pulse;
                }
                else
                {
                    row.localPosition = new Vector3(0f, baseHeight, 0f);
                    row.localScale = Vector3.one;
                }
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
            {
                _armedEnemyRows.Remove(unitId);
                return;
            }

            // An enemy row (built from a committed intent) reads as "armed" → restless in LateUpdate.
            if (icons[0].IsEnemyIntent)
                _armedEnemyRows.Add(unitId);
            else
                _armedEnemyRows.Remove(unitId);

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
            _armedEnemyRows.Clear();
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
            _armedEnemyRows.Remove(unitId);
        }

        private void CreateIcon(Transform row, PlanIconModel model, Vector3 localOffset)
        {
            var iconGo = new GameObject($"Ability_{model.AbilityId}");
            iconGo.transform.SetParent(row, false);
            iconGo.transform.localPosition = localOffset;

            // Ability icon (the committed-move `»` glyph was retired in D3 — a board arrow reads the move).
            // Sprite lives on a scaled child so the hover collider on the icon root keeps its world size.
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

            var collider = iconGo.AddComponent<BoxCollider>();
            collider.size = new Vector3(TelegraphStyle.IconWorldSize, TelegraphStyle.IconWorldSize, 0.1f);
            collider.isTrigger = true;

            iconGo.AddComponent<AbilityIconMarker>()
                .Initialize(model.UnitId, model.QueueIndex, model.IsEnemyIntent);
        }
    }
}
