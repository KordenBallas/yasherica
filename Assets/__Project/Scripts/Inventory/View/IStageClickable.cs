namespace Inventory.View
{
    /// <summary>
    /// Anything on the 3D inventory stage that receives routed pointer clicks
    /// (pot bubbles, blank sockets). The stage drag router raycasts explicitly
    /// from the overlay camera and calls <see cref="NotifyClicked"/> on the hit.
    /// </summary>
    public interface IStageClickable
    {
        void NotifyClicked();
    }
}
