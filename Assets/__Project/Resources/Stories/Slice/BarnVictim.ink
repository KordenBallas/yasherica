// "The Looted Barn - Victim" - window-1 victim encounter for the barn-demo partition (D5/D15).
// A farmer whose barn was raided pleads for help. The choice writes world.barn_quest_accepted, which
// (with world.grain_recovered from the raider fight) routes window 2 to the grateful-farmer (A) or
// starving-village (C) reaction. Parameterized and free of hardcoded NPCs (R4); systems reached ONLY
// via tags. The runner injects npc_name on fresh start (W2-2/W2-4).

VAR npc_name = ""

=== start ===
# speaker: {npc_name}
# fact: world.barn_quest_offered Set true
"Raiders cleaned out my barn. My little ones won't see spring."
* [I'll bring your grain back.]
    # fact: world.barn_quest_accepted Set true
    "The gods walk with you. He fled up the road."
* [Not my trouble.]
    # fact: world.barn_quest_accepted Set false
    "...No. No, I suppose it isn't."
- -> END
