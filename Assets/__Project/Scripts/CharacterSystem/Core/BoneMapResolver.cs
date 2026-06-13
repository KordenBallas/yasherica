using System.Collections.Generic;

namespace CharacterSystem.Core
{
    /// <summary>
    /// Resolves a part's ordered bone-name list against a skeleton's bone set.
    /// Matching is exact (ordinal, case-sensitive) by the project's naming convention.
    /// </summary>
    public sealed class BoneMapResolver
    {
        public BoneMapResult Resolve(IReadOnlyList<string> partBoneNames, SkeletonData skeleton)
        {
            if (partBoneNames == null || partBoneNames.Count == 0 || skeleton == null)
            {
                return new BoneMapResult(null, null);
            }

            List<string> missing = null;
            foreach (var boneName in partBoneNames)
            {
                if (!skeleton.HasBone(boneName))
                {
                    missing ??= new List<string>();
                    missing.Add(boneName);
                }
            }

            return new BoneMapResult(partBoneNames, missing);
        }
    }
}
