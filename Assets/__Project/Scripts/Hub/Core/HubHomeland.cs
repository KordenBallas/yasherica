using LevelGeneration;

namespace Hub.Core
{
    /// <summary>
    /// One enterable homeland on the Hub (O1 rework): the entry theme its portal launches into,
    /// the portal's signboard label, and the short F-prompt name. Derived from the race roster.
    /// </summary>
    public sealed class HubHomeland
    {
        public HubHomeland(LevelTheme theme, string label, string promptName)
        {
            Theme = theme;
            Label = label ?? string.Empty;
            PromptName = promptName ?? string.Empty;
        }

        public LevelTheme Theme { get; }

        /// <summary>The portal signboard (e.g. "Forest — Fox-folk homeland").</summary>
        public string Label { get; }

        /// <summary>The short name the F prompt shows (e.g. "Forest").</summary>
        public string PromptName { get; }
    }
}
