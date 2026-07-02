namespace Core.Logging
{
    /// <summary>
    /// Logging abstraction so domain/application code never touches UnityEngine.Debug.
    /// (Named IGameLogger to avoid clashing with UnityEngine.ILogger.)
    /// Every call carries a <see cref="LogCategory"/> so output can be muted or raised per system.
    /// </summary>
    public interface IGameLogger
    {
        void Info(LogCategory category, string message);
        void Warning(LogCategory category, string message);
        void Error(LogCategory category, string message);
    }
}
