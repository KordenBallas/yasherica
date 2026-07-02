namespace Inventory.View
{
    /// <summary>
    /// Contract for the detached inventory stage: the off-world diorama holding
    /// the cauldron, rendered by its own overlay camera stacked on the main one.
    /// </summary>
    public interface IInventoryStageView
    {
        /// <summary>
        /// Activates or deactivates the stage's overlay camera. While inactive the
        /// stage renders nothing and its raycaster ignores clicks.
        /// </summary>
        void SetStageActive(bool active);
    }
}
