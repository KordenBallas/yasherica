using System;
using System.Collections.Generic;

namespace CharacterSystem.Core
{
    /// <summary>
    /// Outcome of resolving a part's ordered bone-name list against a skeleton.
    /// BoneNames preserves the part's mesh-index order (the bindpose contract).
    /// </summary>
    public sealed class BoneMapResult
    {
        public IReadOnlyList<string> BoneNames { get; }
        public IReadOnlyList<string> MissingBoneNames { get; }

        public bool IsValid => BoneNames.Count > 0 && MissingBoneNames.Count == 0;

        public BoneMapResult(IReadOnlyList<string> boneNames, IReadOnlyList<string> missingBoneNames)
        {
            BoneNames = boneNames ?? Array.Empty<string>();
            MissingBoneNames = missingBoneNames ?? Array.Empty<string>();
        }
    }
}
