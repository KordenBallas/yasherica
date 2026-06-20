using System.Collections.Generic;
using Narrative.Dialogue.Core;
using Narrative.Quests.Core;

namespace Narrative.Casting.Core
{
    /// <summary>
    /// The pool of orthogonal fragments a casting can recombine: dialogues, quests, and combat
    /// enemies. Matching is by semantic tags (R5) — a fragment fills a slot when it carries ALL of
    /// the slot's required tags. Matches are returned in a stable id order so a seeded tie-break is
    /// reproducible (D1).
    /// </summary>
    public interface IFragmentLibrary
    {
        IReadOnlyList<DialogueData> FindDialogues(IReadOnlyList<string> requiredTags);
        IReadOnlyList<QuestData> FindQuests(IReadOnlyList<string> requiredTags);
        IReadOnlyList<EnemyFragment> FindEnemies(IReadOnlyList<string> requiredTags);
    }

    /// <summary>Default in-memory <see cref="IFragmentLibrary"/>.</summary>
    public sealed class FragmentLibrary : IFragmentLibrary
    {
        private readonly List<DialogueData> _dialogues;
        private readonly List<QuestData> _quests;
        private readonly List<EnemyFragment> _enemies;

        public FragmentLibrary(IEnumerable<DialogueData> dialogues, IEnumerable<QuestData> quests, IEnumerable<EnemyFragment> enemies)
        {
            _dialogues = dialogues != null ? new List<DialogueData>(dialogues) : new List<DialogueData>();
            _quests = quests != null ? new List<QuestData>(quests) : new List<QuestData>();
            _enemies = enemies != null ? new List<EnemyFragment>(enemies) : new List<EnemyFragment>();
        }

        public IReadOnlyList<DialogueData> FindDialogues(IReadOnlyList<string> requiredTags)
        {
            var matches = new List<DialogueData>();
            foreach (var d in _dialogues)
            {
                if (HasAllTags(d.Tags, requiredTags))
                {
                    matches.Add(d);
                }
            }

            matches.Sort((a, b) => string.CompareOrdinal(a.DialogueId, b.DialogueId));
            return matches;
        }

        public IReadOnlyList<QuestData> FindQuests(IReadOnlyList<string> requiredTags)
        {
            var matches = new List<QuestData>();
            foreach (var q in _quests)
            {
                if (HasAllTags(q.Tags, requiredTags))
                {
                    matches.Add(q);
                }
            }

            matches.Sort((a, b) => string.CompareOrdinal(a.QuestId, b.QuestId));
            return matches;
        }

        public IReadOnlyList<EnemyFragment> FindEnemies(IReadOnlyList<string> requiredTags)
        {
            var matches = new List<EnemyFragment>();
            foreach (var e in _enemies)
            {
                if (HasAllTags(e.Tags, requiredTags))
                {
                    matches.Add(e);
                }
            }

            matches.Sort((a, b) => string.CompareOrdinal(a.EnemyId, b.EnemyId));
            return matches;
        }

        private static bool HasAllTags(IReadOnlyList<string> fragmentTags, IReadOnlyList<string> requiredTags)
        {
            if (requiredTags == null || requiredTags.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < requiredTags.Count; i++)
            {
                if (!Contains(fragmentTags, requiredTags[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Contains(IReadOnlyList<string> tags, string tag)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                if (string.Equals(tags[i], tag, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
