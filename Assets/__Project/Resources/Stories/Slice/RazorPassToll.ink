// "The Toll at Razor Pass" - bandit shakedown dialogue for the procedural narrative slice.
// Parameterized and free of hardcoded NPCs/items (R4). Game systems are reached ONLY via tags:
//   # speaker: <name>        -> presentation
//   # fact: <ns>.<key> <op> <value>  -> writes a fact through the runner (footprint-gated)
//   # offer-quest: <slotTag> -> starts the casting's quest
//   # start-combat: <slotTag> -> suspends; the runner resumes after combat reports back
// The runner injects these variables on fresh start (W2-2/W2-4).

VAR npc_name = ""
VAR combat_won = false
VAR quest_accepted = false
VAR quest_available = false
VAR combat_available = false

=== start ===
# speaker: {npc_name}
Pay the toll, or bleed.
+ [Pay the toll.] -> pay
+ {quest_available} [Run a job for them instead.] -> deal
+ {combat_available} [Draw steel.] -> fight

=== pay ===
# fact: world.pass_cleared Set true
The road is yours, traveller.
-> END

=== deal ===
# offer-quest: errand
{ quest_accepted:
    "Clear the wolves from the gully and the pass is yours."
- else:
    They spit and wave you off.
}
-> END

=== fight ===
// The start-combat tag is the last tag in this step; the runner suspends here and
// resumes at combat_result once the combat outcome is reported (B1).
# start-combat: bandit
-> combat_result

=== combat_result ===
{ combat_won:
    # fact: world.pass_cleared Set true
    You stand over the bandit. The pass is clear.
- else:
    Beaten, you stagger back the way you came.
}
-> END
