namespace Core.Logging
{
    /// <summary>
    /// Severity of a log line, ordered from least to most verbose. A system's configured
    /// level is a ceiling: a line is emitted only when its severity is at or below that ceiling.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>Silence: nothing is emitted for the system.</summary>
        Off = 0,
        /// <summary>Only errors.</summary>
        Error = 1,
        /// <summary>Errors and warnings.</summary>
        Warning = 2,
        /// <summary>Everything, including informational logs.</summary>
        Info = 3
    }
}
