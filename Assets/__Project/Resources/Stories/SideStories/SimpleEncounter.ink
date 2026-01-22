// Simple Battle/Peace SideStory
// Demonstrates state transitions from dialogue to combat or idle/completed states

// === Variables ===
VAR encounter_resolved = false

// === External Functions ===
EXTERNAL trigger_combat(enemy_count)

// Stub for external function (used when testing in Inky editor)
=== function trigger_combat(enemy_count) ===
~ return

// === Story Start ===
=== start ===
# speaker: Mysterious Wanderer
Halt! You dare trespass here?

+ [I mean no harm.]
    -> proposition_calm
+ [Get out of my way!]
    -> proposition_aggressive

// === Level 2: Proposition (Calm approach) ===
=== proposition_calm ===
# speaker: Mysterious Wanderer
Hmm... I sense no malice in your words. Very well. We can settle this with words... or with steel. What will it be?

+ [Let's talk this out.]
    -> peace_resolution
+ [Actually, I'm ready to fight!]
    -> battle_resolution

// === Level 2: Proposition (Aggressive approach) ===
=== proposition_aggressive ===
# speaker: Mysterious Wanderer
Bold words! I admire your courage, fool. We can settle this with words... or with steel. What will it be?

+ [Wait, let's talk about this.]
    -> peace_resolution
+ [Draw your weapon!]
    -> battle_resolution

// === Peace Resolution ===
=== peace_resolution ===
# speaker: Mysterious Wanderer
Wise choice. Not all conflicts need bloodshed. Pass, traveler, and may your journey be peaceful.
~ encounter_resolved = true
-> END

// === Battle Resolution ===
=== battle_resolution ===
# speaker: Mysterious Wanderer
So be it! Prepare yourself!
# outcome: Combat
~ trigger_combat(1)
~ encounter_resolved = true
-> END
