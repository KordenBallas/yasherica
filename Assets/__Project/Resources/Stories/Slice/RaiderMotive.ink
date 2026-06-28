// "The Raider's Counter-Offer" - barn_raid thread, the cross-actor MORAL FORK (quest-as-reward §4,
// D10-D13). The SAME raider instance, recast because he still carries actor.$self.looted_barn from
// the barn raid. He now OFFERS a competing quest (tag raider-run, carried by this story's optional
// Quest slot): run grain to HIS starving kin for iron - the power/Monster side, opposed to the
// farmer's bounty taken earlier (world.barn_quest_accepted). Separated in time on the shared actor's
// thread, NOT shown side by side. Same tier, different currency (facts are the stakes, not loot-EV).
// Accepting sets world.raider_offer_taken and clears actor.$self.looted_barn so the arc closes (D13).
// Leaving is the presenter's system Leave card (he lingers, may re-offer). NPC-free (R4); systems via
// tags only. npc_name injected on fresh start.

VAR npc_name = ""
VAR quest_accepted = false

=== start ===
# speaker: {npc_name}
Ты снова встречаешь налётчика — мешок всё ещё у него за спиной. «Опять ты. Слушай: моя деревня тоже голодает. Доставь мешок моим родичам, и я заплачу добрым железом.»
* [Отнести зерно для налётчика.]
    # fact: world.raider_offer_taken Set true
    # fact: actor.$self.looted_barn Set false
    # offer-quest: raider-run
    «Умно. Верность дороже хлеба.» Он отмечает тебе дорогу.
- -> END
