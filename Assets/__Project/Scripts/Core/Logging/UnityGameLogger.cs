using UnityEngine;

namespace Core.Logging
{
    /// <summary>
    /// Infrastructure adapter forwarding the logging abstraction to UnityEngine.Debug.
    /// Consults the <see cref="LogLevelPolicy"/> first, so a system muted in the config asset
    /// produces no Console output. Lines are prefixed with their category to keep them greppable.
    /// </summary>
    public class UnityGameLogger : IGameLogger
    {
        private readonly LogLevelPolicy _policy;

        public UnityGameLogger(LogLevelPolicy policy)
        {
            _policy = policy;
        }

        public void Info(LogCategory category, string message)
        {
            if (_policy.ShouldLog(category, LogLevel.Info))
            {
                Debug.Log(Format(category, message));
            }
        }

        public void Warning(LogCategory category, string message)
        {
            if (_policy.ShouldLog(category, LogLevel.Warning))
            {
                Debug.LogWarning(Format(category, message));
            }
        }

        public void Error(LogCategory category, string message)
        {
            if (_policy.ShouldLog(category, LogLevel.Error))
            {
                Debug.LogError(Format(category, message));
            }
        }

        private static string Format(LogCategory category, string message) => $"[{category}] {message}";
    }
}
