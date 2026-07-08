using System.Collections.Generic;
using Loot.Core;
using MetaProgression.Core;

namespace Loot.Data
{
    /// <summary>
    /// Projects the authored artifact/blank catalogs into the pure quest-reward pools (P1-5),
    /// applying the meta-progression gate (Track R, FR3): a meta-gated token whose deed is unmet
    /// never enters the reward roll. A null vocabulary (a scene without the meta bindings) filters
    /// nothing; unmarked tokens always pass (FR15).
    /// </summary>
    public static class QuestRewardPoolsBuilder
    {
        public static QuestRewardPools Build(
            Inventory.Data.IArtifactCatalog artifacts,
            Mutation.Core.IPartBlankDataSource blanks,
            IMetaVocabulary vocabulary)
        {
            var artifactOptions = new List<RewardArtifactOption>();
            if (artifacts?.All != null)
            {
                foreach (var definition in artifacts.All)
                {
                    if (definition == null)
                    {
                        continue;
                    }

                    if (vocabulary != null
                        && !vocabulary.IsUnlocked(definition.Id, definition.MetaGating.ToCore()))
                    {
                        continue;
                    }

                    artifactOptions.Add(new RewardArtifactOption(
                        definition.Id, definition.Tier, definition.RewardFamilyId));
                }
            }

            var blankOptions = new List<RewardBlankOption>();
            if (blanks?.All != null)
            {
                foreach (var blank in blanks.All)
                {
                    if (blank == null)
                    {
                        continue;
                    }

                    if (vocabulary != null && !vocabulary.IsUnlocked(blank.DefinitionId, blank.Gate))
                    {
                        continue;
                    }

                    blankOptions.Add(new RewardBlankOption(blank.DefinitionId, blank.RaceId));
                }
            }

            return new QuestRewardPools(artifactOptions, blankOptions);
        }
    }
}
