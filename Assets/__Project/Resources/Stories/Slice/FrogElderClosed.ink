// "The Marsh Elder - Closed Door" - frog_marsh thread, passport NEGATIVE (D16). Eligible while
// faction.fox.reads_as_tier < 1: the elder turns the unmarked outsider away, offering no quest.
// Proves doors stay shut to a stranger. Recurs until the passport tier rises (intended - the marsh
// keeps shooing you) and naturally stops once a fox-tagged part is worn and reads_as_tier >= 1
// routes to FrogElderOpen instead (races-passport.md). A pure reaction beat (like StarvingVillage).
// NPC-free (R4). npc_name injected on start.

VAR npc_name = ""

=== start ===
# speaker: {npc_name}
Старейшина лисьего народца щурится на твою гладкую шкуру. «Нам не о чем говорить с бесприметными. Возвращайся на свою дорогу, чужак.»
-> END
