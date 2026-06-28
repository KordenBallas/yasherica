// "The Grateful Farmer" - window-2 reaction A. Eligible when the player accepted the plea AND recovered
// the grain (world.barn_quest_accepted == true AND world.grain_recovered == true). A reward beat that
// proves the world reacts to the combined window-1 choices AND closes the cross-dialogue quest loop: the
// "Farmer's Bounty" quest (tag `bounty`) was OFFERED on the window-1 victim platform; the runner restores
// that live instance here (the story's optional Quest slot carries the same quest), so this dialogue only
// advances its objective and completes it - the item reward is granted on platform completion.
// NPC-free (R4); systems reached via tags only. The runner injects npc_name on fresh start (W2-2/W2-4).

VAR npc_name = ""
VAR quest_accepted = false

=== start ===
# speaker: {npc_name}
"Every sack returned, just as you swore."
# advance-objective: obj_return_grain
# complete-quest: qst_barn_bounty
"Take this purse - it's all I can spare, and my thanks beyond it."
-> END
