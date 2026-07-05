// "The Marsh Pool" - frog_marsh thread, passport HINT (D15). A marsh hermit reads the outsider's
// body and names the rule of the road in: the marsh opens to those who wear the fox's marks. The
// passport is no longer card-set - the equipped body projects into faction.fox.reads_as_tier
// (races-passport.md, written by RacePassportProjector); this beat only teaches the rule. Eligible
// while the hero does not yet read as fox-kin (tier < 1), so it stops recurring once a fox-tagged
// part is worn. NPC-free (R4); systems reached via tags only. npc_name injected on start.

VAR npc_name = ""

=== start ===
# speaker: {npc_name}
Болотный отшельник наблюдает за тобой из камышей. «Гладкокожий. Болото открывается лишь тем, на ком лисья примета. Отрасти её - или добудь.»
* [Запомнить совет.]
    Камыши шелестят, будто соглашаясь.
- -> END
