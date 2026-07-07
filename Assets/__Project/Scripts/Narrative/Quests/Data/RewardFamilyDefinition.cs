using UnityEngine;

namespace Narrative.Quests.Data
{
    /// <summary>
    /// One coarse function family of the quest-reward economy (P1-5 / P0-3·b), e.g. power/combat vs
    /// utility/access: its stable id (matched by <c>ArtifactDefinition.RewardFamilyId</c> and quest
    /// reward declarations) and the belonging colour the offer-card grammar tints with. Contains ONLY
    /// configuration data — NO logic. Adding a family = authoring one of these assets under
    /// Resources/Rewards/; no code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "RewardFamily_", menuName = "Narrative/Quests/Reward Family")]
    public class RewardFamilyDefinition : ScriptableObject
    {
        [Tooltip("Stable id referenced by artifact definitions and quest reward declarations (e.g. 'power'). Lowercase, no spaces.")]
        [SerializeField] private string _familyId;

        [Tooltip("Name shown in UI (e.g. 'Power').")]
        [SerializeField] private string _displayName;

        [Tooltip("Belonging colour for the card grammar (tint = belonging), the artifact-family counterpart of RaceDefinition's belonging colour.")]
        [SerializeField] private Color _belongingColor = Color.white;

        public string FamilyId => _familyId;
        public string DisplayName => _displayName;
        public Color BelongingColor => _belongingColor;
    }
}
