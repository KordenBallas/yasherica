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

        // On-unit status row (S2): sits BELOW the plan-icon row so the two reads never collide.
        public const string StatusIconsRowName = "StatusIcons";
        public const float StatusIconsHeightAboveUnit = 1.9f;
        public const float StatusIconSpacing = 0.38f;
        public const float StatusIconWorldSize = 0.3f;
        public const float StatusTurnsFontSize = 3f;
        public const float StatusTurnsOffsetX = 0.14f;
        public const float StatusTurnsOffsetY = -0.12f;

        // Ghost playback
        public const float GhostFadeInSeconds = 0.2f;
        public const float GhostHoldSeconds = 1.0f;
        public const float GhostFadeOutSeconds = 0.4f;
        public const float GhostAlpha = 0.35f;
        public const float DamageLabelFontSize = 4f;
        public const float DamageLabelHeight = 1.6f;

        // Ability sweep animation (D3): a cell-flash swept along a line / popped across a ring, played
        // translucent as the ghost preview and opaque on live execution. Kept short so an enemy action
        // reads within the paced resolve beat (EnemyRoundController ~0.4s between intents).
        public const float AbilitySweepSeconds = 0.3f;
        public const float AbilityFlashCellSize = 0.9f;      // ~one hex; placeholder footprint of a struck cell
        public const float AbilityFlashLiveAlpha = 0.85f;    // opaque-ish for real execution
        // The ghost/translucent treatment reuses GhostAlpha.

        // Enemy readiness cue (D3): restless plan icons + a wind-up body pose while an enemy holds a queue.
        public const float ReadyIconJitterAmplitude = 0.04f;
        public const float ReadyIconPulseAmplitude = 0.12f;  // fraction added to icon-row scale at the pulse peak
        public const float ReadyCuePulseSpeed = 6f;          // radians/sec for the jitter+pulse+bob sine
        public const float ReadyPoseLeanDistance = 0.12f;    // forward lean applied to the enemy visual root
        public const float ReadyPoseScaleBoost = 0.06f;      // fraction added to the enemy's scale while armed
        public const float ReadyPoseBobAmplitude = 0.05f;    // gentle vertical bob
        public const float ReadyPoseTweenSpeed = 8f;         // how fast the pose eases in/out

        // Enemy move-direction arrow (D3): a SHORT pointer from the hex centre toward the shared edge in
        // the move direction (not a full centre-to-centre arrow). Length is a fraction of the distance to
        // the destination cell, so 0.5 lands exactly on the edge between the two hexes.
        public const float MoveArrowYOffset = 0.08f;
        public const float MoveArrowLengthFraction = 0.5f;
        public const float MoveArrowWidth = 0.07f;
        public const float MoveArrowHeadLength = 0.16f;
        public const float MoveArrowHeadWidth = 0.18f;
    }
}
