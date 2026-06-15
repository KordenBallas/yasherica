using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Data.Definitions;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Inventory.View
{
    /// <summary>
    /// Thin adapter for the feeding UI: world-space tray bubbles above the pot plus a uGUI readout
    /// panel (cumulative archetype weights, digestion progress bar, dominant archetypes) and the
    /// Feed button. Forwards tray clicks and the Feed click to the presenter; renders the data the
    /// presenter pushes in. Holds no feeding logic.
    /// </summary>
    public class FeedingView : MonoBehaviour, IFeedingView
    {
        [Header("Tray")]
        [Tooltip("Parent transform the tray bubbles are laid out under")]
        [SerializeField] private Transform _trayAnchor;
        [SerializeField] private BubbleView _artifactItemPrefab;
        [Tooltip("Local spacing between adjacent tray bubbles")]
        [SerializeField] private float _traySpacing = 0.35f;

        [Header("Panel")]
        [Tooltip("Root toggled with the feeding mode")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button _feedButton;
        [SerializeField] private Text _cumulativeLabel;
        [SerializeField] private Text _dominantLabel;
        [SerializeField] private Text _progressLabel;
        [Tooltip("Filled image showing digestion progress toward the threshold")]
        [SerializeField] private Image _progressFill;

        [Inject] private InventoryConfig _config;

        private readonly List<BubbleView> _trayViews = new List<BubbleView>();

        public event Action<int> OnTrayItemClicked;
        public event Action OnFeedClicked;

        private void Awake()
        {
            if (_feedButton != null)
            {
                _feedButton.onClick.AddListener(HandleFeedButton);
            }
        }

        private void OnDestroy()
        {
            if (_feedButton != null)
            {
                _feedButton.onClick.RemoveListener(HandleFeedButton);
            }
        }

        public void ShowTray(IReadOnlyList<ArtifactViewData> items)
        {
            ClearTrayViews();

            float offset = (items.Count - 1) * 0.5f;
            for (int i = 0; i < items.Count; i++)
            {
                var view = Instantiate(_artifactItemPrefab, _trayAnchor);
                view.transform.localPosition = new Vector3((i - offset) * _traySpacing, 0f, 0f);
                view.Configure(items[i].InstanceId, items[i].Icon, items[i].Tint, GetDepthSettings());
                view.SetInteractable(true);
                view.OnClicked += HandleTrayItemClicked;
                _trayViews.Add(view);
            }
        }

        public void SetFeedEnabled(bool enabled)
        {
            if (_feedButton != null)
            {
                _feedButton.interactable = enabled;
            }
        }

        public void SetCumulativeReadout(IReadOnlyList<ArchetypeReadoutEntry> entries)
        {
            if (_cumulativeLabel != null)
            {
                _cumulativeLabel.text = FormatEntries(entries, "Nothing selected");
            }
        }

        public void SetProgression(int fed, int threshold, float normalized)
        {
            if (_progressFill != null)
            {
                _progressFill.fillAmount = Mathf.Clamp01(normalized);
            }

            if (_progressLabel != null)
            {
                string ready = fed >= threshold ? "  (ready to mutate)" : string.Empty;
                _progressLabel.text = $"Digestion: {fed}/{threshold}{ready}";
            }
        }

        public void SetDominant(IReadOnlyList<ArchetypeReadoutEntry> dominant)
        {
            if (_dominantLabel != null)
            {
                _dominantLabel.text = FormatEntries(dominant, "No archetype yet");
            }
        }

        public void SetVisible(bool visible)
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(visible);
            }

            if (!visible)
            {
                ClearTrayViews();
            }
        }

        private void HandleFeedButton()
        {
            OnFeedClicked?.Invoke();
        }

        private void HandleTrayItemClicked(int instanceId)
        {
            OnTrayItemClicked?.Invoke(instanceId);
        }

        private void ClearTrayViews()
        {
            foreach (var view in _trayViews)
            {
                if (view != null)
                {
                    view.OnClicked -= HandleTrayItemClicked;
                    Destroy(view.gameObject);
                }
            }

            _trayViews.Clear();
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

        private static string FormatEntries(IReadOnlyList<ArchetypeReadoutEntry> entries, string emptyText)
        {
            if (entries == null || entries.Count == 0)
            {
                return emptyText;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('\n');
                }

                var entry = entries[i];
                string hex = ColorUtility.ToHtmlStringRGB(entry.Tint);
                builder.Append($"<color=#{hex}>{entry.DisplayName}</color>  {entry.Weight:0.##}");
            }

            return builder.ToString();
        }
    }
}
