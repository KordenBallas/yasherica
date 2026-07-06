using System;
using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Player;
using UnityEngine;

namespace Combat.View
{
    /// <summary>
    /// Renders the enemy committed-intent board telegraph (D3): a ground **move-direction arrow** toward
    /// the committed destination, and the **"armed / about to act" wind-up pose** on the enemy's visual
    /// root (a placeholder transform tween — lean + scale + bob — since the rig has no attack state). Thin
    /// adapter driven by <see cref="EnemyIntentTelegraphPresenter"/>; one per combat, created/disposed by
    /// <c>CombatActiveState</c>. Uses only position/scale on the enemy root, never rotation
    /// (<c>UnitFacingRotator</c> owns that), and clears the moment an enemy's intent resolves.
    /// </summary>
    public sealed class EnemyIntentTelegraphView : MonoBehaviour, IEnemyIntentTelegraphView
    {
        private static readonly Color ArrowTint = new Color(1f, 0.5f, 0.35f, 0.95f);
        private const float MinArrowSqrDistance = 0.0025f;

        private sealed class PoseState
        {
            public Transform Target;
            public Vector3 BaseScale;
            public Vector3 AppliedOffset; // our current additive position contribution (never an absolute pin)
            public float Weight;
            public bool Armed;
        }

        private ICombatUnitViewRegistry _registry;
        private Func<HexCoordinates, Vector3> _hexToWorld;
        private Material _arrowMaterial;

        private readonly Dictionary<int, LineRenderer> _arrows = new Dictionary<int, LineRenderer>();
        private readonly Dictionary<int, PoseState> _poses = new Dictionary<int, PoseState>();

        public void Initialize(ICombatUnitViewRegistry registry, Func<HexCoordinates, Vector3> hexToWorld)
        {
            _registry = registry;
            _hexToWorld = hexToWorld;
            _arrowMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        public void SetTelegraphs(IReadOnlyList<EnemyIntentTelegraphModel> telegraphs)
        {
            var armed = new HashSet<int>();
            var moving = new HashSet<int>();

            if (telegraphs != null)
            {
                foreach (var model in telegraphs)
                {
                    if (!model.IsArmed)
                        continue;

                    armed.Add(model.UnitId);
                    TrackPose(model.UnitId);

                    if (model.HasMove && _hexToWorld != null)
                    {
                        moving.Add(model.UnitId);
                        UpdateArrow(model);
                    }
                }
            }

            // Arrows whose enemy is no longer committing a move drop out.
            RemoveArrowsExcept(moving);

            // Poses whose enemy is no longer armed ease back out (removed in Update once at rest).
            foreach (var kv in _poses)
                kv.Value.Armed = armed.Contains(kv.Key);
        }

        public void Clear()
        {
            foreach (var arrow in _arrows.Values)
            {
                if (arrow != null)
                    Destroy(arrow.gameObject);
            }

            _arrows.Clear();

            foreach (var pose in _poses.Values)
                RestorePose(pose);

            _poses.Clear();
        }

        private void Update()
        {
            if (_poses.Count == 0)
                return;

            var toRemove = new List<int>();
            foreach (var kv in _poses)
            {
                var pose = kv.Value;
                if (pose.Target == null)
                {
                    toRemove.Add(kv.Key);
                    continue;
                }

                float target = pose.Armed ? 1f : 0f;
                pose.Weight = Mathf.MoveTowards(pose.Weight, target, TelegraphStyle.ReadyPoseTweenSpeed * Time.deltaTime);
                ApplyPose(pose);

                if (!pose.Armed && pose.Weight <= 0.001f)
                {
                    RestorePose(pose);
                    toRemove.Add(kv.Key);
                }
            }

            foreach (var key in toRemove)
                _poses.Remove(key);
        }

        private void OnDestroy()
        {
            Clear();
            if (_arrowMaterial != null)
                Destroy(_arrowMaterial);
        }

        // --- pose ---

        private void TrackPose(int unitId)
        {
            if (_poses.TryGetValue(unitId, out var existing))
            {
                existing.Armed = true; // keep the captured base mid-tween
                return;
            }

            if (_registry == null || !_registry.TryGet(unitId, out var target) || target == null)
                return;

            _poses[unitId] = new PoseState
            {
                Target = target,
                BaseScale = target.localScale,
                AppliedOffset = Vector3.zero,
                Weight = 0f,
                Armed = true
            };
        }

        /// <summary>
        /// Applies the wind-up as an additive offset: first removes our previous contribution, then adds
        /// the current one, so the pose composes with wherever the game logic put the unit (movement /
        /// push) and never pins an absolute position.
        /// </summary>
        private void ApplyPose(PoseState pose)
        {
            pose.Target.position -= pose.AppliedOffset;

            float w = Mathf.SmoothStep(0f, 1f, pose.Weight);
            float bob = Mathf.Sin(Time.time * TelegraphStyle.ReadyCuePulseSpeed) * TelegraphStyle.ReadyPoseBobAmplitude * w;

            var forward = pose.Target.forward; // rotates with facing (UnitFacingRotator owns rotation)
            var offset = forward * (TelegraphStyle.ReadyPoseLeanDistance * w) + Vector3.up * bob;

            pose.Target.position += offset;
            pose.AppliedOffset = offset;
            pose.Target.localScale = pose.BaseScale * (1f + TelegraphStyle.ReadyPoseScaleBoost * w);
        }

        private static void RestorePose(PoseState pose)
        {
            if (pose.Target == null)
                return;

            pose.Target.position -= pose.AppliedOffset; // leave the unit exactly where the game put it
            pose.AppliedOffset = Vector3.zero;
            pose.Target.localScale = pose.BaseScale;
        }

        // --- arrow ---

        private void UpdateArrow(EnemyIntentTelegraphModel model)
        {
            var fromWorld = _hexToWorld(model.MoveFrom);
            var toWorld = _hexToWorld(model.MoveTo);

            var full = toWorld - fromWorld;
            full.y = 0f;
            if (full.sqrMagnitude < MinArrowSqrDistance)
            {
                RemoveArrow(model.UnitId);
                return;
            }

            // A short pointer from the hex centre toward the shared edge in the move direction — not a
            // full centre-to-centre arrow (0.5 of the distance = exactly on the edge between the hexes).
            var dir = full.normalized;
            float length = full.magnitude * TelegraphStyle.MoveArrowLengthFraction;
            var perp = Vector3.Cross(dir, Vector3.up).normalized;
            float y = TelegraphStyle.MoveArrowYOffset;

            var start = new Vector3(fromWorld.x, y, fromWorld.z);
            var tip = start + dir * length;
            var headBase = tip - dir * TelegraphStyle.MoveArrowHeadLength;
            var headLeft = headBase + perp * (TelegraphStyle.MoveArrowHeadWidth * 0.5f);
            var headRight = headBase - perp * (TelegraphStyle.MoveArrowHeadWidth * 0.5f);

            var line = GetOrCreateArrow(model.UnitId);
            // One continuous stroke: shaft → tip → head-left → tip → head-right (the retrace fills the V).
            line.positionCount = 5;
            line.SetPosition(0, start);
            line.SetPosition(1, tip);
            line.SetPosition(2, headLeft);
            line.SetPosition(3, tip);
            line.SetPosition(4, headRight);
        }

        private LineRenderer GetOrCreateArrow(int unitId)
        {
            if (_arrows.TryGetValue(unitId, out var existing) && existing != null)
                return existing;

            var go = new GameObject($"MoveArrow_{unitId}");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.material = _arrowMaterial;
            line.startColor = ArrowTint;
            line.endColor = ArrowTint;
            line.startWidth = TelegraphStyle.MoveArrowWidth;
            line.endWidth = TelegraphStyle.MoveArrowWidth;
            line.numCapVertices = 2;
            line.textureMode = LineTextureMode.Stretch;
            _arrows[unitId] = line;
            return line;
        }

        private void RemoveArrow(int unitId)
        {
            if (_arrows.TryGetValue(unitId, out var line))
            {
                if (line != null)
                    Destroy(line.gameObject);
                _arrows.Remove(unitId);
            }
        }

        private void RemoveArrowsExcept(HashSet<int> keep)
        {
            var toRemove = new List<int>();
            foreach (var key in _arrows.Keys)
            {
                if (!keep.Contains(key))
                    toRemove.Add(key);
            }

            foreach (var key in toRemove)
                RemoveArrow(key);
        }
    }
}
