using System.Collections.Generic;

namespace GameInput.Core
{
    /// <summary>
    /// The declarative source of truth for the action vocabulary's per-source bindings and prompt cues
    /// (Input Foundation R1/R2). Everything that talks about bindings reads this table: the prompt layer
    /// (via <see cref="PromptCueProvider"/>), the dev overlay, the coverage test, and the drift-guard
    /// test that keeps the runtime <c>GameActions.inputactions</c> asset in sync with it. Pure C# and
    /// immutable so it is unit-testable without Unity; rebinding UI is out of scope, so the table is
    /// code, not a ScriptableObject.
    /// </summary>
    public sealed class InputBindingCatalog
    {
        /// <summary>Touch rows carry no control path: they are satisfied by the on-screen overlay
        /// (stick + interact button) or by direct pointer taps through the UI event system.</summary>
        private const string OnScreen = "";

        private static readonly IReadOnlyList<BindingEntry> Rows = new List<BindingEntry>
        {
            // Move
            new BindingEntry(GameAction.Move, InputSource.KeyboardMouse, "<Keyboard>/w", "WASD"),
            new BindingEntry(GameAction.Move, InputSource.Gamepad, "<Gamepad>/leftStick", "LS"),
            new BindingEntry(GameAction.Move, InputSource.Touch, OnScreen, "Stick"),

            // Interact — the world talk/enter verb
            new BindingEntry(GameAction.Interact, InputSource.KeyboardMouse, "<Keyboard>/f", "F"),
            new BindingEntry(GameAction.Interact, InputSource.Gamepad, "<Gamepad>/buttonNorth", "Y"),
            new BindingEntry(GameAction.Interact, InputSource.Touch, OnScreen, "Tap"),

            // Confirm / select
            new BindingEntry(GameAction.Confirm, InputSource.KeyboardMouse, "*/{Submit}", "Enter"),
            new BindingEntry(GameAction.Confirm, InputSource.Gamepad, "<Gamepad>/buttonSouth", "A"),
            new BindingEntry(GameAction.Confirm, InputSource.Touch, OnScreen, "Tap"),

            // Cancel / back
            new BindingEntry(GameAction.Cancel, InputSource.KeyboardMouse, "*/{Cancel}", "Esc"),
            new BindingEntry(GameAction.Cancel, InputSource.Gamepad, "<Gamepad>/buttonEast", "B"),
            new BindingEntry(GameAction.Cancel, InputSource.Touch, OnScreen, "Tap"),

            // Navigate — focus between options/cards/menu items
            new BindingEntry(GameAction.Navigate, InputSource.KeyboardMouse, "<Keyboard>/upArrow", "Arrows"),
            new BindingEntry(GameAction.Navigate, InputSource.Gamepad, "<Gamepad>/dpad", "D-Pad"),
            new BindingEntry(GameAction.Navigate, InputSource.Touch, OnScreen, "Tap"),

            // Aim — pointing the active combat gesture
            new BindingEntry(GameAction.Aim, InputSource.KeyboardMouse, "<Mouse>/position", "Mouse"),
            new BindingEntry(GameAction.Aim, InputSource.Gamepad, "<Gamepad>/leftStick", "LS"),

            // Fire — commit the queued volley (tap = forward, hold = aim first)
            new BindingEntry(GameAction.Fire, InputSource.KeyboardMouse, "<Keyboard>/enter", "Enter"),
            new BindingEntry(GameAction.Fire, InputSource.Gamepad, "<Gamepad>/rightTrigger", "RT"),

            // Combat movement mode (hold → aim → release to confirm)
            new BindingEntry(GameAction.CombatMoveMode, InputSource.KeyboardMouse, "<Keyboard>/m", "M"),
            new BindingEntry(GameAction.CombatMoveMode, InputSource.Gamepad, "<Gamepad>/leftTrigger", "LT"),

            // Combat facing change
            new BindingEntry(GameAction.CombatChangeDirection, InputSource.KeyboardMouse, "<Keyboard>/s", "S"),
            new BindingEntry(GameAction.CombatChangeDirection, InputSource.Gamepad, "<Gamepad>/buttonWest", "X"),

            // Ability slots 1-6. On gamepad the d-pad doubles as Navigate; combat and menu focus are
            // never live at the same time, so the overlap is deliberate.
            new BindingEntry(GameAction.AbilitySlot1, InputSource.KeyboardMouse, "<Keyboard>/q", "Q"),
            new BindingEntry(GameAction.AbilitySlot1, InputSource.Gamepad, "<Gamepad>/dpad/up", "D-Up"),
            new BindingEntry(GameAction.AbilitySlot2, InputSource.KeyboardMouse, "<Keyboard>/w", "W"),
            new BindingEntry(GameAction.AbilitySlot2, InputSource.Gamepad, "<Gamepad>/dpad/right", "D-Right"),
            new BindingEntry(GameAction.AbilitySlot3, InputSource.KeyboardMouse, "<Keyboard>/e", "E"),
            new BindingEntry(GameAction.AbilitySlot3, InputSource.Gamepad, "<Gamepad>/dpad/down", "D-Down"),
            new BindingEntry(GameAction.AbilitySlot4, InputSource.KeyboardMouse, "<Keyboard>/r", "R"),
            new BindingEntry(GameAction.AbilitySlot4, InputSource.Gamepad, "<Gamepad>/dpad/left", "D-Left"),
            new BindingEntry(GameAction.AbilitySlot5, InputSource.KeyboardMouse, "<Keyboard>/t", "T"),
            new BindingEntry(GameAction.AbilitySlot5, InputSource.Gamepad, "<Gamepad>/leftShoulder", "LB"),
            new BindingEntry(GameAction.AbilitySlot6, InputSource.KeyboardMouse, "<Keyboard>/y", "Y"),
            new BindingEntry(GameAction.AbilitySlot6, InputSource.Gamepad, "<Gamepad>/rightShoulder", "RB")
        };

        /// <summary>
        /// Touch combat is a dedicated interaction brief (how you aim and volley with fingers is
        /// design, not plumbing) — the whole combat gesture family is consciously deferred there.
        /// </summary>
        private static readonly IReadOnlyList<DeferredBindingGap> Gaps = new List<DeferredBindingGap>
        {
            new DeferredBindingGap(GameAction.Aim, InputSource.Touch,
                "Combat touch interaction is its own design brief", "ROADMAP: Input / touch combat brief"),
            new DeferredBindingGap(GameAction.Fire, InputSource.Touch,
                "Combat touch interaction is its own design brief", "ROADMAP: Input / touch combat brief"),
            new DeferredBindingGap(GameAction.CombatMoveMode, InputSource.Touch,
                "Combat touch interaction is its own design brief", "ROADMAP: Input / touch combat brief"),
            new DeferredBindingGap(GameAction.CombatChangeDirection, InputSource.Touch,
                "Combat touch interaction is its own design brief", "ROADMAP: Input / touch combat brief"),
            new DeferredBindingGap(GameAction.AbilitySlot1, InputSource.Touch,
                "Combat touch interaction is its own design brief", "ROADMAP: Input / touch combat brief"),
            new DeferredBindingGap(GameAction.AbilitySlot2, InputSource.Touch,
                "Combat touch interaction is its own design brief", "ROADMAP: Input / touch combat brief"),
            new DeferredBindingGap(GameAction.AbilitySlot3, InputSource.Touch,
                "Combat touch interaction is its own design brief", "ROADMAP: Input / touch combat brief"),
            new DeferredBindingGap(GameAction.AbilitySlot4, InputSource.Touch,
                "Combat touch interaction is its own design brief", "ROADMAP: Input / touch combat brief"),
            new DeferredBindingGap(GameAction.AbilitySlot5, InputSource.Touch,
                "Combat touch interaction is its own design brief", "ROADMAP: Input / touch combat brief"),
            new DeferredBindingGap(GameAction.AbilitySlot6, InputSource.Touch,
                "Combat touch interaction is its own design brief", "ROADMAP: Input / touch combat brief")
        };

        public IReadOnlyList<BindingEntry> Entries => Rows;

        public IReadOnlyList<DeferredBindingGap> DeferredGaps => Gaps;

        /// <summary>The cue text a prompt shows for <paramref name="action"/> on <paramref name="source"/>,
        /// or null when the pair is unbound (a deferred gap).</summary>
        public string GetCue(GameAction action, InputSource source)
        {
            for (int i = 0; i < Rows.Count; i++)
            {
                var row = Rows[i];
                if (row.Action == action && row.Source == source)
                {
                    return row.Cue;
                }
            }

            return null;
        }

        public bool IsBound(GameAction action, InputSource source)
        {
            return GetCue(action, source) != null;
        }
    }
}
