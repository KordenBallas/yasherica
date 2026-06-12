using System.Collections.Generic;

namespace Inventory.Core
{
    /// <summary>
    /// Result of the cauldron profile calculation: outer and inner wall polylines
    /// running bottom-to-rim. The lists are index-paired (same count, matching wall
    /// position) so a mesh builder can close the cross-section cut planes with
    /// simple quad strips between corresponding points.
    /// </summary>
    public sealed class CauldronProfile
    {
        public IReadOnlyList<ProfilePoint> Outer { get; }
        public IReadOnlyList<ProfilePoint> Inner { get; }

        public CauldronProfile(IReadOnlyList<ProfilePoint> outer, IReadOnlyList<ProfilePoint> inner)
        {
            Outer = outer;
            Inner = inner;
        }
    }
}
