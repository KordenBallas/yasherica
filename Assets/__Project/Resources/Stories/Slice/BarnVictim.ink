// "The Looted Barn - Victim" - window-1 victim encounter for the barn-demo partition (D5/D15).
// A farmer whose barn was raided pleads for help. The single quest-offer choice writes
// world.barn_quest_accepted and OFFERS the "Farmer's Bounty" quest (tag `bounty`, carried by this story's
// optional Quest slot): the runner mints + registers the live instance here so a later platform (the
// grateful farmer) can complete it - cross-dialogue quest continuity. Walking away is the card-hand's
// system Leave card, not an Ink choice (world.barn_quest_accepted then stays at its default false, which
// routes window 2 to the starving-village reaction C). Encounter card model: Ink carries only the
// quest-offer reply; Leave is added by the presenter. Parameterized and free of hardcoded NPCs (R4);
// systems reached ONLY via tags. npc_name injected on fresh start.

VAR npc_name = ""

=== start ===
# speaker: {npc_name}
# fact: world.barn_quest_offered Set true
"[[Raiders]] cleaned out my barn. My little ones won't see spring."
* [I'll bring your grain back.]
    # fact: world.barn_quest_accepted Set true
    # offer-quest: bounty
    "The gods walk with you. He fled up the road."
- -> END
