using System;
using System.Text;
using Narrative.QuestLog.Core;
using Narrative.Quests.Core;
using Narrative.View;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Narrative.QuestLog.View
{
    /// <summary>
    /// Thin MonoBehaviour adapter for the quest log / saga readout (P1-11): polls the toggle key
    /// (the same view-layer input-adapter pattern as <c>NpcInteractionInput</c>) and paints the
    /// model as rich text — sagas with their lifecycle verdict, quests grouped Active / Completed /
    /// Failed-Expired inside each, reward telegraphs as tier pips tinted with the belonging colour
    /// and a hidden "?" — never the rolled item. Read-only presentation; the exact panel art is a
    /// later render-look pass.
    /// </summary>
    public class QuestLogView : MonoBehaviour, IQuestLogView
    {
        [Tooltip("A CHILD panel root toggled with visibility (never this component's own object).")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TMP_Text _bodyText;
        [Tooltip("Toggle key for opening/closing the log.")]
        [SerializeField] private Key _toggleKey = Key.J;

        [Header("Grammar colours")]
        [SerializeField] private Color _sagaHeaderColor = new Color(0.95f, 0.87f, 0.6f);
        [SerializeField] private Color _mutedColor = new Color(0.62f, 0.6f, 0.58f);
        [SerializeField] private Color _tierGlowColor = new Color(1f, 0.85f, 0.4f);

        // Belonging id -> authored colour (P0-3·b), resolved at the view boundary.
        [Inject] private IBelongingTintCatalog _belongingTints;

        public event Action OnToggleRequested;

        public bool IsVisible => _panelRoot != null && _panelRoot.activeSelf;

        private void Awake()
        {
            Hide();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[_toggleKey].wasPressedThisFrame)
            {
                OnToggleRequested?.Invoke();
            }
        }

        public void Show(QuestLogModel model)
        {
            if (_bodyText != null)
            {
                _bodyText.text = Compose(model ?? QuestLogModel.Empty);
            }

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }
        }

        public void Hide()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }
        }

        private string Compose(QuestLogModel model)
        {
            if (model.Sagas.Count == 0)
            {
                return $"<color=#{Hex(_mutedColor)}>Пока ни одного дела не взято.</color>";
            }

            var builder = new StringBuilder();
            for (int i = 0; i < model.Sagas.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append("\n\n");
                }

                AppendSaga(builder, model.Sagas[i]);
            }

            return builder.ToString();
        }

        private void AppendSaga(StringBuilder builder, QuestSaga saga)
        {
            if (!string.IsNullOrEmpty(saga.ThreadId))
            {
                builder.Append("<color=#").Append(Hex(_sagaHeaderColor)).Append("><b>")
                    .Append(SagaTitle(saga.ThreadId)).Append("</b></color>")
                    .Append("  <color=#").Append(Hex(_mutedColor)).Append('>')
                    .Append(SagaStateLabel(saga.State)).Append("</color>\n");
            }

            for (int i = 0; i < saga.Entries.Count; i++)
            {
                AppendEntry(builder, saga.Entries[i]);
            }
        }

        private void AppendEntry(StringBuilder builder, QuestLogEntry entry)
        {
            builder.Append(StateGlyph(entry.State)).Append(" <b>").Append(entry.Title).Append("</b>");
            if (entry.HasRewardTelegraph)
            {
                builder.Append("  ").Append(RewardTelegraph(entry));
            }

            builder.Append('\n');
            if (!string.IsNullOrEmpty(entry.GiverName))
            {
                builder.Append("<color=#").Append(Hex(_mutedColor)).Append(">   — ")
                    .Append(entry.GiverName).Append("</color>\n");
            }

            if (entry.State == QuestState.Active)
            {
                for (int i = 0; i < entry.Objectives.Count; i++)
                {
                    var objective = entry.Objectives[i];
                    builder.Append("   ").Append(objective.Completed ? "☑ " : "☐ ")
                        .Append(objective.Description);
                    if (objective.TargetCount > 1)
                    {
                        builder.Append(" (").Append(objective.Progress).Append('/')
                            .Append(objective.TargetCount).Append(')');
                    }

                    builder.Append('\n');
                }
            }
        }

        /// <summary>Tier pips in the glow colour + a belonging-tinted hidden "?" — the same promise
        /// the offer card made, never the rolled item (the log is not a reward catalog).</summary>
        private string RewardTelegraph(QuestLogEntry entry)
        {
            var pips = new string('✦', Mathf.Clamp(entry.RewardTier + 1, 1, 5));
            var tint = _belongingTints != null
                ? _belongingTints.TintFor(entry.BelongingId)
                : Color.white;
            return $"<color=#{Hex(_tierGlowColor)}>{pips}</color><color=#{Hex(tint)}>?</color>";
        }

        private static string StateGlyph(QuestState state)
        {
            switch (state)
            {
                case QuestState.Completed: return "✔";
                case QuestState.Failed: return "✖";
                default: return "▶";
            }
        }

        private static string SagaTitle(string threadId) => threadId.Replace('_', ' ');

        private static string SagaStateLabel(QuestSagaState state)
        {
            switch (state)
            {
                case QuestSagaState.Completed: return "(сага завершена)";
                case QuestSagaState.FailedByConflict: return "(сага оборвана — ты выбрал другую сторону)";
                case QuestSagaState.Foreclosed: return "(сага оборвана — её больше некому вести)";
                case QuestSagaState.Expired: return "(сага угасла)";
                case QuestSagaState.Live: return "(идёт)";
                default: return string.Empty;
            }
        }

        private static string Hex(Color color) => ColorUtility.ToHtmlStringRGB(color);
    }
}
