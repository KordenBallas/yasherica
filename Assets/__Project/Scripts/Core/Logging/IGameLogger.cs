namespace Core.Logging
{
    /// <summary>
    /// Logging abstraction so domain/application code never touches UnityEngine.Debug.
    /// (Named IGameLogger to avoid clashing with UnityEngine.ILogger.)
    /// </summary>
    public interface IGameLogger
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message);
    }
}
