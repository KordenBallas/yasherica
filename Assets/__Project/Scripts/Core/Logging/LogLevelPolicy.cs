using System.Collections.Generic;

namespace Core.Logging
{
    /// <summary>
    /// Pure, Unity-free decision object: given a system and a severity, decides whether the line
    /// should be emitted. Built from <see cref="LoggingConfig"/> at install time (the Data → Core
    /// mapper), so the filtering rule itself stays unit-testable without Unity.
    /// </summary>
    public sealed class LogLevelPolicy
    {
        private readonly LogLevel _masterLevel;
        private readonly LogLevel _defaultCategoryLevel;
        private readonly IReadOnlyDictionary<LogCategory, LogLevel> _categoryLevels;

        public LogLevelPolicy(
            LogLevel masterLevel,
            LogLevel defaultCategoryLevel,
            IReadOnlyDictionary<LogCategory, LogLevel> categoryLevels)
        {
            _masterLevel = masterLevel;
            _defaultCategoryLevel = defaultCategoryLevel;
            _categoryLevels = categoryLevels ?? new Dictionary<LogCategory, LogLevel>();
        }

        /// <summary>Permissive fallback used when no config asset is present: everything at Info.</summary>
        public static LogLevelPolicy AllEnabled() =>
            new LogLevelPolicy(LogLevel.Info, LogLevel.Info, new Dictionary<LogCategory, LogLevel>());

        /// <summary>
        /// True when a line of <paramref name="level"/> from <paramref name="category"/> passes both
        /// the per-system ceiling and the global master ceiling.
        /// </summary>
        public bool ShouldLog(LogCategory category, LogLevel level)
        {
            if (level == LogLevel.Off)
            {
                return false;
            }

            if (level > _masterLevel)
            {
                return false;
            }

            var ceiling = _categoryLevels.TryGetValue(category, out var configured)
                ? configured
                : _defaultCategoryLevel;

            return level <= ceiling;
        }
    }
}
