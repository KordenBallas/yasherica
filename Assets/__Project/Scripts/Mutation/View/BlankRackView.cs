using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mutation.View
{
    /// <summary>
    /// World-space adapter for the operating-table rack left of the cauldron:
    /// instantiates one blank entry per racked blank at its anchor (from a
    /// disabled template) and forwards the entries' drop/click events. No logic.
    /// </summary>
    public class BlankRackView : MonoBehaviour, IBlankRackView
    {
        [Header("Anchors")]
        [Tooltip("Where racked blanks appear, top to bottom; the rack capacity should not exceed this")]
        [SerializeField] private Transform[] _blankAnchors;

        [Header("Template")]
        [Tooltip("Disabled blank-entry template cloned per racked blank")]
        [SerializeField] private BlankEntryView _entryTemplate;

        private readonly List<BlankEntryView> _entries = new List<BlankEntryView>();

        public event Action<int, int> OnArtifactDroppedOnBlank;
        public event Action<int, int> OnFilledSocketClicked;

        public void ShowBlanks(IReadOnlyList<BlankEntryViewData> blanks)
        {
            ClearEntries();

            if (_entryTemplate == null || _blankAnchors == null)
            {
                return;
            }

            for (int i = 0; i < blanks.Count && i < _blankAnchors.Length; i++)
            {
                var entry = Instantiate(_entryTemplate, _blankAnchors[i]);
                entry.transform.localPosition = Vector3.zero;
                entry.gameObject.SetActive(true);
                entry.Configure(blanks[i]);
                entry.OnArtifactDropped += HandleArtifactDropped;
                entry.OnFilledSocketClicked += HandleFilledSocketClicked;
                _entries.Add(entry);
            }
        }

        private void ClearEntries()
        {
            foreach (var entry in _entries)
            {
                if (entry != null)
                {
                    entry.OnArtifactDropped -= HandleArtifactDropped;
                    entry.OnFilledSocketClicked -= HandleFilledSocketClicked;
                    Destroy(entry.gameObject);
                }
            }

            _entries.Clear();
        }

        private void HandleArtifactDropped(int artifactInstanceId, int blankInstanceId)
        {
            OnArtifactDroppedOnBlank?.Invoke(artifactInstanceId, blankInstanceId);
        }

        private void HandleFilledSocketClicked(int blankInstanceId, int artifactInstanceId)
        {
            OnFilledSocketClicked?.Invoke(blankInstanceId, artifactInstanceId);
        }
    }
}
