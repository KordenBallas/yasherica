// "The Gorge Toll" - a sellsword guarding the gorge demands a crossing fee. New-engine tag syntax only:
//   # speaker: <name>  / # fact: <ns>.<key> <op> <value>  / # start-combat: <slotTag>  / # outcome: <label>
// Writes world.gorge_cleared on resolve (opens the warden follow-up). The runner injects these VARs.

VAR npc_name = ""
VAR combat_won = false
VAR quest_accepted = false
VAR quest_available = false
VAR combat_available = false

=== start ===
# speaker: {npc_name}
This gorge is mine to guard. Coin, or cross steel.
+ [Pay the crossing fee.] -> pay
+ {combat_available} [Refuse, and draw.] -> fight

=== pay ===
# fact: world.gorge_cleared Set true
Smart. The gorge road is open to you.
-> END

=== fight ===
// start-combat is the last tag in this step; the runner suspends and resumes at combat_result (B1).
# start-combat: bandit
-> combat_result

=== combat_result ===
{ combat_won:
    # fact: world.gorge_cleared Set true
    The sellsword yields. The gorge is yours.
- else:
    Bested, you fall back from the gorge.
}
-> END
