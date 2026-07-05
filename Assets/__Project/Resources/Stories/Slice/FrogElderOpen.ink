// "The Marsh Elder - Open Door" - frog_marsh thread, passport POSITIVE (D16). Eligible when
// faction.fox.reads_as_tier >= 1 AND world.frog_quest_offered == false: wearing at least one
// fox-tagged part, the hero reads as marsh-kin and the elder OFFERS the "Marsh Elder's Errand"
// quest (tag frog-errand, carried by this story's optional Quest slot). The tier crossing 0 -> 1
// (a fox part equipped, projected by RacePassportProjector) is what swaps this story in for
// FrogElderClosed - the passport flip, now read off the real body (races-passport.md). On meeting
// sets world.frog_quest_offered (guards recurrence, mirroring barn_quest_offered); the accept card
// sets world.frog_quest_accepted and offers the quest. Leaving is the presenter's system Leave card
// (no Ink choice). NPC-free (R4). npc_name injected on start.

VAR npc_name = ""
VAR quest_accepted = false

=== start ===
# speaker: {npc_name}
# fact: world.frog_quest_offered Set true
Старейшина лисьего народца оглядывает рыжую примету на твоём теле и кивает как родичу. «Свой! Отнесёшь это к дальнему пруду, братец?»
* [Я выполню твоё поручение.]
    # fact: world.frog_quest_accepted Set true
    # offer-quest: frog-errand
    «Болото помнит доброго родича. Беги шустро.»
- -> END
