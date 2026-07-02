using System.Collections.Generic;
using UnityEngine;

namespace Core.Logging
{
    /// <summary>
    /// Designer-facing switchboard for runtime logging. One row per system with a verbosity ceiling,
    /// plus a global master ceiling, so a feature can be tested with only its own logs visible.
    /// Configuration data only; the single behaviour here is the explicit Data → Core mapper
    /// (<see cref="ToPolicy"/>) that §7 of the project rules sanctions.
    /// </summary>
    [CreateAssetMenu(fileName = "LoggingConfig", menuName = "Config/Logging Config")]
    public class LoggingConfig : ScriptableObject
    {
        [System.Serializable]
        public struct CategorySetting
        {
            public LogCategory Category;

            [Tooltip("Most verbose level emitted for this system. " +
                     "Off = silent, Error = only errors, Warning = errors + warnings, Info = everything.")]
            public LogLevel MaxLevel;
        }

        [Header("Master")]
        [Tooltip("Global ceiling applied on top of every system. Off silences all logs at once.")]
        [SerializeField] private LogLevel _masterLevel = LogLevel.Info;

        [Tooltip("Ceiling used for any system not listed below.")]
        [SerializeField] private LogLevel _defaultCategoryLevel = LogLevel.Info;

        [Header("Per-system ceilings")]
        [SerializeField] private List<CategorySetting> _categories = new List<CategorySetting>();

        /// <summary>Maps the authored data into the pure, Unity-free <see cref="LogLevelPolicy"/>.</summary>
        public LogLevelPolicy ToPolicy()
        {
            var map = new Dictionary<LogCategory, LogLevel>();
            foreach (var setting in _categories)
            {
                map[setting.Category] = setting.MaxLevel;
            }

            return new LogLevelPolicy(_masterLevel, _defaultCategoryLevel, map);
        }
    }
}
