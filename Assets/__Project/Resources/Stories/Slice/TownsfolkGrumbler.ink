// "Townsfolk Grumbler" - ambient settlement colour (world-sites brief, NPC-townsfolk fill flavor).
// A townsman grumbles about taxes and the fair; no quest, no facts, no combat - intent Plain.
// Always eligible (no preconditions); fills site Npc slots by the "townsfolk" story tag.
// NPC-free (R4); npc_name injected on start.

VAR npc_name = ""

=== start ===
# speaker: {npc_name}
«Ярмарка, ярмарка... Налог на мост подняли, а моста как не было, так и нет. Зато флажки повесили.»
-> END
