namespace Loot.Application
{
    /// <summary>
    /// Decides whether the inventory can accept another artifact.
    /// The pot inventory is currently unlimited, so the only implementation
    /// always accepts; a future bounded inventory rebinds this interface.
    /// </summary>
    public interface IInventoryCapacityPolicy
    {
        bool CanAccept(string artifactId);
    }
}
