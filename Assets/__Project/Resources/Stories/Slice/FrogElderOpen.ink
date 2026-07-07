// "The Marsh Elder - Open Door" - frog_marsh thread, passport POSITIVE (D16). Eligible when
// faction.fox.reads_as_tier >= 1 AND world.frog_quest_offered == false: wearing at least one
// fox-tagged part, the hero reads as marsh-kin and the elder OFFERS his trouble. Shape A (P1-9):
// ONE situation, SEVERAL resolutions shown together - the errand (tag frog-errand, utility
// currency) and the heron guard job (tag frog-guard, power currency), SAME tier, different
// belonging - "pick your currency, not the bigger number". Each offer choice carries its
// offer-quest tag both ON the choice (labels the card pre-pick) and IN the branch (mints the
// picked instance); picking one resolves the situation, the other is not also taken. On meeting
// sets world.frog_quest_offered (guards recurrence); either accept sets world.frog_quest_accepted.
// Leaving is the presenter's system Leave card (no Ink choice). NPC-free (R4). npc_name injected
// on start.

VAR npc_name = ""
VAR quest_accepted = false

=== start ===
# speaker: {npc_name}
# fact: world.frog_quest_offered Set true
Старейшина лисьего народца оглядывает рыжую примету на твоём теле и кивает как родичу. «Свой! У болота две беды, братец. Выбирай, какую возьмёшь.»
* [Отнести свёрток к дальнему пруду.] # offer-quest: frog-errand
    # fact: world.frog_quest_accepted Set true
    # offer-quest: frog-errand
    «Болото помнит доброго родича. Беги шустро.»
* [Прогнать цапель с малькового плёса.] # offer-quest: frog-guard
    # fact: world.frog_quest_accepted Set true
    # offer-quest: frog-guard
    «Клювастые таскают мальков каждое утро. Напугай их так, чтобы забыли дорогу.»
- -> END
