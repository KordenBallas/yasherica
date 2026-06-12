namespace Character
{
    /// <summary>
    /// Allows other subsystems (e.g. the inventory) to suspend player movement input
    /// without knowing how the character is controlled.
    /// </summary>
    public interface IMovementInputLock
    {
        void SetMovementEnabled(bool enabled);
    }
}
