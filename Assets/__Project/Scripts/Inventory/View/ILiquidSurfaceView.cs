namespace Inventory.View
{
    /// <summary>
    /// Adapter contract for the cauldron liquid: the presenter drives only the
    /// fill height (bowl-local Y of the waterline, snapshotted per open
    /// session); the surface, cut-away curtain, and waterline rebuild follow.
    /// </summary>
    public interface ILiquidSurfaceView
    {
        void SetFillHeight(float height);
    }
}
