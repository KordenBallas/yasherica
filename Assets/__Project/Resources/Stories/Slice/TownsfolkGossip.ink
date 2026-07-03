// "Townsfolk Gossip" - ambient settlement colour (world-sites brief, NPC-townsfolk fill flavor).
// A villager shares local gossip; no quest, no facts, no combat - NpcIntentResolver derives Plain.
// Always eligible (no preconditions); the planner fills site Npc slots with it by the "townsfolk"
// story tag and never offers it on the quest channel. NPC-free (R4); npc_name injected on start.

VAR npc_name = ""

=== start ===
# speaker: {npc_name}
«Слышал? У мельника опять коза пропала. Он на болотных грешит, а я говорю — сама ушла. От него и жена ушла.»
-> END
