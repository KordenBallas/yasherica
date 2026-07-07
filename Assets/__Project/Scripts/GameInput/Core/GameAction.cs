namespace GameInput.Core
{
    /// <summary>
    /// The fixed action vocabulary the game is played through (Input Foundation R1): gameplay and UI
    /// code reads these named actions, never concrete keys or buttons. Extending the vocabulary means
    /// adding a value here plus its per-source rows in <see cref="InputBindingCatalog"/> — the coverage
    /// test then enforces that every source binds it.
    /// </summary>
    public enum GameAction
    {
        /// <summary>Exploration movement (WASD / left stick / on-screen stick).</summary>
        Move,

        /// <summary>The world interaction verb — the overhead "talk / enter" prompt on NPCs and portals.</summary>
        Interact,

        /// <summary>Confirm / select the focused option (menus, cards, dialogs).</summary>
        Confirm,

        /// <summary>Cancel / back out (menus, inventory close, combat gesture abort).</summary>
        Cancel,

        /// <summary>Move focus between options / cards / menu items.</summary>
        Navigate,

        /// <summary>Combat aiming — pointing the current gesture (mouse position / left stick).</summary>
        Aim,

        /// <summary>Fire / commit the queued combat volley (tap fires forward, hold aims first).</summary>
        Fire,

        /// <summary>Combat: hold to enter movement mode, release to confirm the move.</summary>
        CombatMoveMode,

        /// <summary>Combat: change the hero's facing without moving.</summary>
        CombatChangeDirection,

        /// <summary>Combat ability slot 1 (hold to aim, release to queue).</summary>
        AbilitySlot1,

        /// <summary>Combat ability slot 2.</summary>
        AbilitySlot2,

        /// <summary>Combat ability slot 3.</summary>
        AbilitySlot3,

        /// <summary>Combat ability slot 4.</summary>
        AbilitySlot4,

        /// <summary>Combat ability slot 5.</summary>
        AbilitySlot5,

        /// <summary>Combat ability slot 6.</summary>
        AbilitySlot6
    }
}
