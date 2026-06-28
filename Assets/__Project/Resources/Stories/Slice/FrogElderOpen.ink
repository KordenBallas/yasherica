// "The Frog Elder - Open Door" - frog_marsh thread, passport POSITIVE (D16). Eligible when
// world.reads_as_frogfolk == true AND world.frog_quest_offered == false: read as kin, the elder
// OFFERS the "Marsh Elder's Errand" quest (tag frog-errand, carried by this story's optional Quest
// slot). The same one fact (reads_as_frogfolk) flipping false->true is what swaps this story in for
// FrogElderClosed - the passport flip. On meeting sets world.frog_quest_offered (guards recurrence,
// mirroring barn_quest_offered); the accept card sets world.frog_quest_accepted and offers the quest.
// Leaving is the presenter's system Leave card (no Ink choice). NPC-free (R4). npc_name injected on start.

VAR npc_name = ""
VAR quest_accepted = false

=== start ===
# speaker: {npc_name}
# fact: world.frog_quest_offered Set true
Лягушачий старейшина сжимает твою перепончатую ладонь как родичу. «Свой! Отнесёшь это к дальнему пруду, братец?»
* [Я выполню твоё поручение.]
    # fact: world.frog_quest_accepted Set true
    # offer-quest: frog-errand
    «Болото помнит доброго родича. Прыгай шустро.»
- -> END
