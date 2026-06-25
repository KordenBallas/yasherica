// "The Grateful Farmer" - window-2 reaction A. Eligible when the player accepted the plea AND recovered
// the grain (world.barn_quest_accepted == true AND world.grain_recovered == true). A reward beat that
// proves the world reacts to the combined window-1 choices. NPC-free (R4); reached via tags only.

VAR npc_name = ""

=== start ===
# speaker: {npc_name}
"Every sack returned, just as you swore. Take this purse - it's all I can spare, and my thanks beyond it."
-> END
