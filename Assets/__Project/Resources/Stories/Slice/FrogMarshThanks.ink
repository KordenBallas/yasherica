// "The Marsh's Thanks" - frog_marsh thread, reaction (the thread's SECOND sequential beat, so the
// director's within-window thread-coherence preference is exercised against barn_raid). Eligible when
// world.frog_quest_accepted == true: the runner restores the live "Marsh Elder's Errand" quest (tag
// frog-errand carried by this story's optional Quest slot), this dialogue advances its objective and
// completes it - the access/passport reward is granted on platform completion. Mirrors GratefulFarmer.
// NPC-free (R4); systems reached via tags only. npc_name injected on start.

VAR npc_name = ""

=== start ===
# speaker: {npc_name}
The elder waits at the pool's edge, pleased. "Done, and done well, cousin."
# advance-objective: obj_frog_errand
# complete-quest: qst_frog_errand
"The marsh owes you a debt. Its doors are yours now."
-> END
