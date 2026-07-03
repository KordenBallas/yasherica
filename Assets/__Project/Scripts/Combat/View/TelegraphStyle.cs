namespace Combat.View
{
    /// <summary>
    /// Sizing and timing constants for the combat telegraph presentation (overhead plan
    /// icons + ghost playback). Promoting these to a config SO is a ROADMAP follow-up.
    /// </summary>
    public static class TelegraphStyle
    {
        // Overhead plan icons
        public const string PlanIconsRowName = "PlanIcons";
        public const float IconsHeightAboveUnit = 2.4f;
        public const float IconSpacing = 0.55f;
        public const float IconWorldSize = 0.45f;
        public const float MoveGlyphFontSize = 4f;

        // Ghost playback
        public const float GhostFadeInSeconds = 0.2f;
        public const float GhostHoldSeconds = 1.0f;
        public const float GhostFadeOutSeconds = 0.4f;
        public const float GhostAlpha = 0.35f;
        public const float DamageLabelFontSize = 4f;
        public const float DamageLabelHeight = 1.6f;
    }
}
