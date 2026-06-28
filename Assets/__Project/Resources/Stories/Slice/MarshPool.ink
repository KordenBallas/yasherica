// "The Marsh Pool" - frog_marsh thread, passport SETTER (D15). A marsh hermit offers to mark the
// outsider as frog-kin. The card sets world.reads_as_frogfolk = true, which flips the marsh's
// reading of the hero and opens FrogElderOpen / closes FrogElderClosed. In the shipping game this
// fact is projected by the mutation system from the body; here a card stands in so the passport test
// is self-contained and deterministic (D21). Eligible only while reads_as_frogfolk == false, so it
// does not recur once taken. NPC-free (R4); systems reached via tags only. npc_name injected on start.

VAR npc_name = ""

=== start ===
# speaker: {npc_name}
A marsh hermit watches you from the reeds. "Smooth-skin. Wear our look, outsider, and the marsh may yet open to you."
* [Take on frog-kin features.]
    # fact: world.reads_as_frogfolk Set true
    Cold marsh-skin prickles across you. The reeds seem to lean in, closer now.
- -> END
