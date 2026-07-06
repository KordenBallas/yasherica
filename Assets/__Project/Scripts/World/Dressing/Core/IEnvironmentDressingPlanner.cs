using Combat.Battlefield;
using LevelGeneration.Surface;
using World.Sites.Core;

namespace World.Dressing.Core
{
    /// <summary>
    /// The one dressing seam the area generator calls per platform: node facts in, deterministic
    /// dressing plan out. A platform with nothing to dress (no bound kit, unknown theme id, empty
    /// pool) gets <see cref="PlatformDressingPlan.Empty"/> — the base layer, never a failure.
    /// </summary>
    public interface IEnvironmentDressingPlanner
    {
        PlatformDressingPlan Plan(
            int nodeId,
            SiteStamp site,
            PlatformContentKind kind,
            PlatformHexSurface surface,
            int battlefieldMinCells);
    }
}
