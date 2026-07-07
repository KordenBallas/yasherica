using System;
using System.Collections;
using System.Collections.Generic;
using Inventory.Data.Definitions;
using UnityEngine;
using Zenject;

namespace Inventory.View
{
    /// <summary>
    /// World-space adapter for the crafting area above the pot rim: staged artifacts
    /// in slot anchors, the merge travel animation, the crafted result with its
    /// pop-in, the success smoke puff, and the drop-into-pot animations.
    /// </summary>
    public class CraftingSlotsView : MonoBehaviour, ICraftingSlotsView
    {
        [Header("Anchors")]
        [Tooltip("Where staged artifacts appear, in selection order")]
        [SerializeField] private Transform[] _slotAnchors;
        [SerializeField] private Transform _resultAnchor;
        [Tooltip("Where a collected result flies to (the pot liquid center)")]
        [SerializeField] private Transform _potDropTarget;

        [Header("Prefabs")]
        [SerializeField] private BubbleView _artifactItemPrefab;

        [Header("Effects")]
        [SerializeField] private ParticleSystem _successPuff;

        [Header("Animation")]
        [Tooltip("Scale curve for the crafted result popping in; keys above 1 give an overshoot bounce")]
        [SerializeField] private AnimationCurve _resultPopCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.7f, 1.15f), new Keyframe(1f, 1f));

        [Inject] private InventoryConfig _config;

        private readonly List<BubbleView> _stagedViews = new List<BubbleView>();
        // Bubbles mid-flight into the pot; they outlive the staged/result lists and
        // must be destroyed explicitly when the stage deactivates mid-animation.
        private readonly List<BubbleView> _droppingViews = new List<BubbleView>();
        private BubbleView _resultView;
        private Coroutine _mergeCoroutine;

        public event Action OnResultClicked;
        public event Action<int> OnStagedItemClicked;
        public event Action OnMergeCompleted;

        private void OnDisable()
        {
            _mergeCoroutine = null;

            foreach (var view in _droppingViews)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            _droppingViews.Clear();
        }

        public void ShowStagedItems(IReadOnlyList<ArtifactViewData> items)
        {
            ClearStagedViews();

            for (int i = 0; i < items.Count && i < _slotAnchors.Length; i++)
            {
                var view = SpawnItem(items[i], _slotAnchors[i]);
                // Staged items are clickable so the player can unstage them
                // (and cancel the craft while the merge is running).
                view.SetInteractable(true);
                view.OnClicked += HandleStagedItemClicked;
                _stagedViews.Add(view);
            }
        }

        public void PlayMergeAnimation()
        {
            StopMerge();
            _mergeCoroutine = StartCoroutine(AnimateMerge());
        }

        public void ShowResult(ArtifactViewData result)
        {
            ClearResultImmediate();

            _resultView = SpawnItem(result, _resultAnchor);
            _resultView.SetInteractable(true);
            _resultView.OnClicked += HandleResultClicked;
            StartCoroutine(AnimateResultPop(_resultView.transform));
        }

        public void ClearResult(bool collected)
        {
            if (_resultView == null)
            {
                return;
            }

            if (collected)
            {
                var view = _resultView;
                _resultView = null;
                view.OnClicked -= HandleResultClicked;
                view.SetInteractable(false);
                _droppingViews.Add(view);
                StartCoroutine(AnimateDropIntoPot(view));
            }
            else
            {
                ClearResultImmediate();
            }
        }

        public void PlaySuccessPuff()
        {
            if (_successPuff == null)
            {
                return;
            }

            _successPuff.transform.position = _resultAnchor.position;
            _successPuff.Play();
        }

        private BubbleView SpawnItem(ArtifactViewData data, Transform anchor)
        {
            var view = Instantiate(_artifactItemPrefab, anchor);
            view.transform.localPosition = Vector3.zero;
            view.Configure(data.InstanceId, data.Icon, data.Tint, GetDepthSettings());
            // Bubble = "in the brew" (Track F zone rule): artifacts above the
            // surface — staged or hovering as the result — are bare.
            view.SetShellVisible(false);
            return view;
        }

        private ArtifactDepthSettings GetDepthSettings()
        {
            return _config != null
                ? new ArtifactDepthSettings(
                    _config.ArtifactLayerCount,
                    _config.ArtifactLayerSpacing,
                    _config.ArtifactBackLayerDarkening)
                : new ArtifactDepthSettings(1, 0f, 1f);
        }

        private void HandleResultClicked(int _)
        {
            OnResultClicked?.Invoke();
        }

        private void HandleStagedItemClicked(int instanceId)
        {
            OnStagedItemClicked?.Invoke(instanceId);
        }

        private void StopMerge()
        {
            if (_mergeCoroutine != null)
            {
                StopCoroutine(_mergeCoroutine);
                _mergeCoroutine = null;
            }
        }

        private void ClearStagedViews()
        {
            // The merge animates staged views, so a rebuild must abort it; the
            // pending OnMergeCompleted is intentionally never raised after this.
            StopMerge();

            foreach (var view in _stagedViews)
            {
                if (view != null)
                {
                    view.OnClicked -= HandleStagedItemClicked;
                    Destroy(view.gameObject);
                }
            }

            _stagedViews.Clear();
        }

        private void ClearResultImmediate()
        {
            if (_resultView != null)
            {
                _resultView.OnClicked -= HandleResultClicked;
                Destroy(_resultView.gameObject);
                _resultView = null;
            }
        }

        private IEnumerator AnimateMerge()
        {
            // One frame of delay guarantees OnMergeCompleted is never raised
            // re-entrantly from inside the presenter's crafting-started handler,
            // even when the configured duration is zero.
            yield return null;

            float duration = _config != null ? _config.MergeDuration : 0f;
            int count = _stagedViews.Count;
            var startPositions = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                startPositions[i] = _stagedViews[i] != null
                    ? _stagedViews[i].transform.position
                    : Vector3.zero;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);

                Vector3 target = _resultAnchor.position;
                for (int i = 0; i < count; i++)
                {
                    if (_stagedViews[i] != null)
                    {
                        _stagedViews[i].transform.position = Vector3.Lerp(startPositions[i], target, t);
                    }
                }

                yield return null;
            }

            for (int i = 0; i < count; i++)
            {
                if (_stagedViews[i] != null)
                {
                    _stagedViews[i].transform.position = _resultAnchor.position;
                }
            }

            _mergeCoroutine = null;
            OnMergeCompleted?.Invoke();
        }

        private IEnumerator AnimateResultPop(Transform target)
        {
            float duration = _config != null ? _config.ResultPopDuration : 0f;
            Vector3 finalScale = target.localScale;

            if (duration <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration && target != null)
            {
                elapsed += Time.deltaTime;
                float t = _resultPopCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
                target.localScale = finalScale * t;
                yield return null;
            }

            if (target != null)
            {
                target.localScale = finalScale;
            }
        }

        private IEnumerator AnimateDropIntoPot(BubbleView view)
        {
            float duration = _config != null ? _config.ResultDropDuration : 0f;
            Vector3 startPosition = view.transform.position;
            Vector3 startScale = view.transform.localScale;
            Vector3 targetPosition = _potDropTarget != null ? _potDropTarget.position : startPosition;

            float elapsed = 0f;
            while (elapsed < duration && view != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Smooth step matches the easing style used by the camera transitions.
                t = t * t * (3f - 2f * t);

                view.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                view.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            _droppingViews.Remove(view);
            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }
    }
}
