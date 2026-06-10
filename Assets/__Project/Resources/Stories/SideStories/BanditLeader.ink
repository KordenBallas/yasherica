// Bandit Leader Encounter
// Can be resolved peacefully or through combat

// === Variables ===
VAR npc_name = ""
VAR bandit_defeated = false

// === External Functions ===
EXTERNAL trigger_combat(enemy_count)

// Stub for external function (used when testing in Inky editor)
=== function trigger_combat(enemy_count) ===
~ return

// === Story Start ===
// Greeting text is provided by BanditLeaderCharacter.ink; this knot starts at choices.
=== start ===
+ [Here, take it. (Give gold)]
    -> give_gold
+ [I won't give you anything!]
    -> refuse

// === Give Gold ===
=== give_gold ===
# speaker: Bandit Leader
Smart choice. Now get out of here before I change my mind.
~ bandit_defeated = true
-> END

// === Refuse ===
=== refuse ===
# speaker: Bandit Leader
Fool! You think you can stand against us?

+ [Maybe we can work something out?]
    -> negotiate
+ [Draw your weapons!]
    -> fight

// === Negotiate ===
=== negotiate ===
# speaker: Bandit Leader
Hmm... you've got guts. Tell you what - help us with a job, and we'll let you pass.
~ bandit_defeated = true
-> END

// === Fight ===
=== fight ===
# speaker: Bandit Leader
You asked for this! Attack!
# outcome: Combat
~ trigger_combat(3)
~ bandit_defeated = true
-> END
