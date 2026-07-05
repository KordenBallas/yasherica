using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Inventory.View
{
    /// <summary>
    /// Thin uGUI adapter for the stored-parts readout: a small corner panel listing the
    /// parts shed by body-plan changes. Hidden while the stash is empty. No interactions
    /// and no business logic — the presenter decides what to show.
    /// </summary>
    public class PartInventoryView : MonoBehaviour, IPartInventoryView
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TMP_Text _itemsLabel;

        private readonly StringBuilder _builder = new StringBuilder();

        private void Awake()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }
        }

        public void ShowParts(IReadOnlyList<PartInventoryItemViewData> items)
        {
            var hasItems = items != null && items.Count > 0;
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(hasItems);
            }

            if (!hasItems || _itemsLabel == null)
            {
                return;
            }

            _builder.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    _builder.Append('\n');
                }

                _builder.Append("• ").Append(items[i].DisplayName);
                if (items[i].Count > 1)
                {
                    _builder.Append(" ×").Append(items[i].Count);
                }
            }

            _itemsLabel.text = _builder.ToString();
        }
    }
}
