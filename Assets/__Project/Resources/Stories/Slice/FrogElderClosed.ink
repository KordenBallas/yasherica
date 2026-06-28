// "The Frog Elder - Closed Door" - frog_marsh thread, passport NEGATIVE (D16). Eligible while
// world.reads_as_frogfolk == false: the elder turns the unmarked outsider away, offering no quest.
// Proves doors stay shut to a stranger. Recurs until the passport flips (intended - the marsh keeps
// shooing you) and naturally stops once reads_as_frogfolk == true routes to FrogElderOpen instead.
// A pure reaction beat (like StarvingVillage). NPC-free (R4). npc_name injected on start.

VAR npc_name = ""

=== start ===
# speaker: {npc_name}
Горловой мешок лягушачьего старейшины раздувается. «Нам не о чем говорить с сухокожими. Возвращайся на свою дорогу, чужак.»
-> END
