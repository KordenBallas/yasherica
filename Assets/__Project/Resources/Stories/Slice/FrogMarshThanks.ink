// "The Marsh's Thanks" - frog_marsh thread, reaction (the thread's SECOND sequential beat, so the
// director's within-window thread-coherence preference is exercised against barn_raid). Eligible when
// world.frog_quest_accepted == true: the runner restores the live "Marsh Elder's Errand" quest (tag
// frog-errand carried by this story's optional Quest slot), this dialogue advances its objective and
// completes it - the access/passport reward is granted on platform completion. Mirrors GratefulFarmer.
// NPC-free (R4); systems reached via tags only. npc_name injected on start.

VAR npc_name = ""

=== start ===
# speaker: {npc_name}
Старейшина ждёт у кромки пруда, довольный. «Сделано, и сделано славно, братец.»
# advance-objective: obj_frog_errand
# complete-quest: qst_frog_errand
«Болото в долгу перед тобой. Его двери отныне открыты тебе.»
-> END
