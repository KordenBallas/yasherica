// "The Grateful Caravan" - the second storylet's dialogue. Its StoryTemplate is ineligible until
// the toll story writes world.pass_cleared = true; this is the felt cross-thread payoff (R7).
// No game-system effects of its own beyond presentation.

VAR npc_name = ""

=== start ===
# speaker: {npc_name}
You there - you cleared Razor Pass! We can finally bring our goods through.
Here, take this for your trouble.
# outcome: trade
-> END
