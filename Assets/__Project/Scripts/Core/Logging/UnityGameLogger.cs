using UnityEngine;

namespace Core.Logging
{
    /// <summary>
    /// Infrastructure adapter forwarding the logging abstraction to UnityEngine.Debug.
    /// </summary>
    public class UnityGameLogger : IGameLogger
    {
        public void Info(string message)
        {
            Debug.Log(message);
        }

        public void Warning(string message)
        {
            Debug.LogWarning(message);
        }

        public void Error(string message)
        {
            Debug.LogError(message);
        }
    }
}
