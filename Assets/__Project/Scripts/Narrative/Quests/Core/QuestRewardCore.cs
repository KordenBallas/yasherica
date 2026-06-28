namespace Narrative.Quests.Core
{
    /// <summary>
    /// An immutable, UnityEngine-free item reward carried by a quest (R-rewards): an artifact
    /// definition id and how many copies are granted on completion. Item rewards only — the legacy
    /// multi-type reward machinery (currency/experience/ability) has no receiving system.
    /// </summary>
    public sealed class QuestRewardCore
    {
        public string ArtifactId { get; }
        public int Count { get; }

        public QuestRewardCore(string artifactId, int count)
        {
            ArtifactId = artifactId ?? string.Empty;
            Count = count < 1 ? 1 : count;
        }
    }
}
