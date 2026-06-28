// "The Looted Barn" - window-1 raider encounter for the barn-demo partition (D11/D16 + D5/D15).
// Meeting the raider writes a world gate (so the raid resolves once) and an actor-scoped fact
// (actor.$self.looted_barn). The card-hand presents the raider's situation, then a hand of cards:
//   - Drop the grain  (tagged `card: attack` -> the Attack card, rendered as the Monster verb but
//     authored in Ink so its combat consequence survives) -> combat; on win, world.grain_recovered = true
//     and looted_barn is cleared (routes window 2 to the grateful-farmer A or starving-village C reaction).
//   - Take a cut      -> world.raider_bribed = true, looted_barn stays true (routes window 2 to THIS SAME
//     raider's motive storylet B - the recurring-actor path).
//   - Leave           -> the presenter's system Leave card (no Ink choice); looted_barn stays true, also
//     routing to B.
// The Attack tag must follow SHOWN choice text (not bracketed) so it lands in the Ink Choice.tags the
// presenter reads. Parameterized and free of hardcoded NPCs (R4); systems reached ONLY via tags. The
// runner injects npc_name on fresh start (W2-2/W2-4) and writes combat_won back after the suspending
// start-combat (B1).

VAR npc_name = ""
VAR combat_won = false

=== start ===
# speaker: {npc_name}
# fact: world.barn_raided Set true
# fact: actor.$self.looted_barn Set true
The raider shoulders a sack of stolen grain. "Nothing here for you. Walk on."
* Drop the grain. # card: attack
    -> demand
* [Take a cut, look away.]
    -> bribe

=== demand ===
# speaker: {npc_name}
"Then bleed."
# start-combat: enemy_bandit_brute
-> resolve

=== resolve ===
{ combat_won:
    # fact: world.grain_recovered Set true
    # fact: actor.$self.looted_barn Set false
    You wrench the grain free. The barn's loss is undone.
- else:
    The raider leaves you in the dust.
}
-> END

=== bribe ===
# fact: world.raider_bribed Set true
He flips you a coin and slips off, grain still on his back.
-> END
