using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CharacterSystem.View
{
    /// <summary>
    /// Thin uGUI adapter for the body-plan demo console: clones an inactive row template
    /// (a "Label" TMP_Text + a "Dropdown" TMP_Dropdown child, wired by the scene builder)
    /// per slot and forwards dropdown picks. No business logic — the presenter decides
    /// what the options are and what a pick means.
    /// </summary>
    public class BodyPlanDemoConsoleView : MonoBehaviour, IBodyPlanDemoConsoleView
    {
        private const string LabelChildName = "Label";

        [SerializeField] private RectTransform _rowContainer;

        [Tooltip("Inactive template row: a 'Label' TMP_Text child + a TMP_Dropdown child.")]
        [SerializeField] private GameObject _rowTemplate;

        private readonly List<GameObject> _rows = new List<GameObject>();

        public event Action<string, string> OnPartPicked;

        public void ShowRows(IReadOnlyList<BodyPlanDemoRowViewData> rows)
        {
            ClearRows();

            if (rows == null || _rowTemplate == null || _rowContainer == null)
            {
                return;
            }

            foreach (var row in rows)
            {
                var rowObject = Instantiate(_rowTemplate, _rowContainer);
                rowObject.name = $"Row_{row.SlotId}";
                rowObject.SetActive(true);
                _rows.Add(rowObject);

                var label = FindLabel(rowObject.transform);
                if (label != null)
                {
                    label.text = row.SlotLabel;
                }

                var dropdown = rowObject.GetComponentInChildren<TMP_Dropdown>(true);
                if (dropdown == null)
                {
                    continue;
                }

                dropdown.ClearOptions();
                dropdown.AddOptions(new List<string>(row.OptionLabels));
                dropdown.SetValueWithoutNotify(Mathf.Clamp(row.SelectedIndex, 0, row.OptionLabels.Count - 1));
                dropdown.RefreshShownValue();

                var slotId = row.SlotId;
                var optionPartIds = row.OptionPartIds;
                dropdown.onValueChanged.AddListener(index =>
                {
                    if (index >= 0 && index < optionPartIds.Count && !string.IsNullOrEmpty(optionPartIds[index]))
                    {
                        OnPartPicked?.Invoke(slotId, optionPartIds[index]);
                    }
                });
            }
        }

        /// <summary>The label sits directly under the row root; a plain GetComponentInChildren
        /// would also match the dropdown's own caption/item texts.</summary>
        private static TMP_Text FindLabel(Transform row)
        {
            var labelTransform = row.Find(LabelChildName);
            return labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;
        }

        private void ClearRows()
        {
            foreach (var row in _rows)
            {
                if (row != null)
                {
                    Destroy(row);
                }
            }

            _rows.Clear();
        }
    }
}
