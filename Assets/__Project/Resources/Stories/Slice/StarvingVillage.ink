// "The Starving Village" - window-2 reaction C. Eligible when the player recovered the grain but turned
// the victim away (world.barn_quest_accepted == false AND world.grain_recovered == true). A bittersweet
// beat that proves the partition has no overlap with reaction A. NPC-free (R4); reached via tags only.

VAR npc_name = ""

=== start ===
# speaker: {npc_name}
"You downed the raider, they say. Pity you never asked whose grain it was - the barn family went hungry."
-> END
