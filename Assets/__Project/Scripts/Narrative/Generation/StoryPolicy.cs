namespace Narrative.Generation
{
    /// <summary>
    /// Defines policy configuration for different story types.
    /// Encapsulates all behavioral differences between Main Story (Chapter) and Side Story.
    /// Pure data class - immutable configuration.
    /// </summary>
    public class StoryPolicy
    {
        /// <summary>
        /// Base priority bonus added during template scoring.
        /// Main stories have higher base priority than side stories.
        /// </summary>
        public int BasePriorityBonus { get; }

        /// <summary>
        /// Bonus points added if IsKeyProgression flag is set.
        /// Main stories receive +20, side stories receive 0.
        /// </summary>
        public int KeyProgressionBonus { get; }

        /// <summary>
        /// Selection order priority. Lower values are selected first.
        /// Main story = 1 (first), Side story = 2 (after main).
        /// </summary>
        public int SelectionOrder { get; }

        /// <summary>
        /// Whether this story type gets primary NPC pool access.
        /// Main stories reserve NPCs first, side stories use remaining pool.
        /// </summary>
        public bool HasPrimaryNpcAccess { get; }

        /// <summary>
        /// Required count of this story type per area.
        /// Main story = 1 (exactly one), Side story = 0 (calculated dynamically).
        /// </summary>
        public int RequiredCountPerArea { get; }

        /// <summary>
        /// Whether generation failure is critical.
        /// Main story failure is critical, side story failure is non-critical.
        /// </summary>
        public bool IsFailureCritical { get; }

        /// <summary>
        /// Whether generation should continue after failure.
        /// Side stories continue to next, main story may halt.
        /// </summary>
        public bool ContinueOnFailure { get; }

        /// <summary>
        /// Log level for failure messages.
        /// Main story = Error, Side story = Warning.
        /// </summary>
        public FailureLogLevel FailureLogLevel { get; }

        /// <summary>
        /// Multiplier applied to CooldownPlatforms from template.
        /// Main stories typically have longer cooldowns.
        /// </summary>
        public float CooldownMultiplier { get; }

        /// <summary>
        /// Whether this story type typically requires specific theme.
        /// Main stories may require theme, side stories are usually flexible.
        /// </summary>
        public bool TypicallyRequiresTheme { get; }

        /// <summary>
        /// Whether this story type typically has strict chapter constraints.
        /// Main stories often have strict MinimumChapter/MaximumChapter.
        /// </summary>
        public bool HasStrictChapterConstraints { get; }

        /// <summary>
        /// Variety bonus for stories not recently played.
        /// </summary>
        public int VarietyBonus { get; }

        /// <summary>
        /// Theme match bonus when template matches current theme.
        /// </summary>
        public int ThemeMatchBonus { get; }

        /// <summary>
        /// Creates a new story policy with specified configuration.
        /// </summary>
        public StoryPolicy(
            int basePriorityBonus,
            int keyProgressionBonus,
            int selectionOrder,
            bool hasPrimaryNpcAccess,
            int requiredCountPerArea,
            bool isFailureCritical,
            bool continueOnFailure,
            FailureLogLevel failureLogLevel,
            float cooldownMultiplier,
            bool typicallyRequiresTheme,
            bool hasStrictChapterConstraints,
            int varietyBonus,
            int themeMatchBonus)
        {
            BasePriorityBonus = basePriorityBonus;
            KeyProgressionBonus = keyProgressionBonus;
            SelectionOrder = selectionOrder;
            HasPrimaryNpcAccess = hasPrimaryNpcAccess;
            RequiredCountPerArea = requiredCountPerArea;
            IsFailureCritical = isFailureCritical;
            ContinueOnFailure = continueOnFailure;
            FailureLogLevel = failureLogLevel;
            CooldownMultiplier = cooldownMultiplier;
            TypicallyRequiresTheme = typicallyRequiresTheme;
            HasStrictChapterConstraints = hasStrictChapterConstraints;
            VarietyBonus = varietyBonus;
            ThemeMatchBonus = themeMatchBonus;
        }
    }

    /// <summary>
    /// Log level for story generation failures.
    /// </summary>
    public enum FailureLogLevel
    {
        /// <summary>
        /// Warning level - non-critical, generation continues.
        /// </summary>
        Warning,

        /// <summary>
        /// Error level - critical failure that may impact game flow.
        /// </summary>
        Error,

        /// <summary>
        /// Critical level - severe error requiring immediate attention.
        /// </summary>
        Critical
    }
}
