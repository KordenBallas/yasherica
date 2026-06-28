using System;
using System.Collections.Generic;
using CharacterProgression.Core;
using DevTools.Core;
using Narrative.Casting.Core;
using Narrative.Facts.Core;

namespace DevTools
{
    /// <summary>
    /// Assembles the developer-overlay sections from the live narrative state: quest statuses (from the
    /// run progression record, with ids mapped to display names via the fragment library) and the
    /// director fact store. Pure C# (no UnityEngine) so the section content is unit-testable.
    /// </summary>
    public sealed class DevStatePresenter : IDevStateSource
    {
        private readonly IRunProgressionRecord _progression;
        private readonly IFragmentLibrary _fragments;
        private readonly IFactStore _factStore;

        public DevStatePresenter(IRunProgressionRecord progression, IFragmentLibrary fragments, IFactStore factStore)
        {
            _progression = progression;
            _fragments = fragments;
            _factStore = factStore;
        }

        public IReadOnlyList<DevPanelSection> BuildSections()
        {
            return new List<DevPanelSection>
            {
                BuildQuestsSection(),
                BuildFactsSection()
            };
        }

        private DevPanelSection BuildQuestsSection()
        {
            var names = BuildQuestNameMap();
            var rows = new List<string>
            {
                $"Active {_progression.ActiveQuests.Count} | Completed {_progression.CompletedQuests.Count} | Failed {_progression.FailedQuests.Count}"
            };

            AppendQuestGroup(rows, "Active", _progression.ActiveQuests, names);
            AppendQuestGroup(rows, "Completed", _progression.CompletedQuests, names);
            AppendQuestGroup(rows, "Failed", _progression.FailedQuests, names);

            if (rows.Count == 1)
            {
                rows.Add("(no quests recorded)");
            }

            return new DevPanelSection("Quests", rows);
        }

        private static void AppendQuestGroup(List<string> rows, string status, IReadOnlyCollection<string> questIds,
            IReadOnlyDictionary<string, string> names)
        {
            // Stable display order regardless of the record's internal set ordering.
            var ordered = new List<string>(questIds);
            ordered.Sort(StringComparer.Ordinal);

            foreach (var id in ordered)
            {
                var name = names.TryGetValue(id, out var displayName) && !string.IsNullOrEmpty(displayName)
                    ? displayName
                    : id;
                rows.Add($"{name} ({id}): {status}");
            }
        }

        private Dictionary<string, string> BuildQuestNameMap()
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            // FindQuests with no required tags returns every quest in the library (HasAllTags(empty) == true).
            var all = _fragments.FindQuests(Array.Empty<string>());
            for (int i = 0; i < all.Count; i++)
            {
                var quest = all[i];
                if (quest != null && !string.IsNullOrEmpty(quest.QuestId))
                {
                    map[quest.QuestId] = quest.DisplayName;
                }
            }

            return map;
        }

        private DevPanelSection BuildFactsSection()
        {
            var snapshot = _factStore.Snapshot();
            var rows = new List<string> { $"{snapshot.Count} fact(s)" };

            foreach (var pair in snapshot)
            {
                rows.Add($"{pair.Key} = {pair.Value} [{pair.Value.Type}]");
            }

            if (snapshot.Count == 0)
            {
                rows.Add("(none set)");
            }

            return new DevPanelSection("Director Facts", rows);
        }
    }
}
