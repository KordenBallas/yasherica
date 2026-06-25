// "Why He Raids" - raider-motive encounter for the recurring-actor demo (D11/D16).
// The SAME raider instance, recast because he still carries actor.$self.looted_barn from the barn
// raid. Hearing his motive closes the arc (clears the fact) so the storylet does not recur.
// Parameterized and free of hardcoded NPCs (R4); game systems are reached ONLY via tags.

VAR npc_name = ""

=== start ===
# speaker: {npc_name}
You meet the raider again on the road. He lowers the grain. "My village starves. I am no thief by trade."
# fact: actor.$self.looted_barn Set false
He shoulders the sack once more and is gone.
-> END
