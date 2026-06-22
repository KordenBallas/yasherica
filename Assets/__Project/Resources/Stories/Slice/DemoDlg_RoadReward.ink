// "The Warden's Reward" - a road warden thanks you for opening the gorge. Presentation only (no fact
// writes); ends with an outcome label. The runner injects npc_name on fresh start.

VAR npc_name = ""

=== start ===
# speaker: {npc_name}
You opened the gorge road - the wardens won't forget it. Here, for your trouble.
# outcome: trade
-> END
